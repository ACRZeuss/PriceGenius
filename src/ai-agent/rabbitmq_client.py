import json
import time
import logging
import pika
from config import Config

logger = logging.getLogger(__name__)


class RabbitMQClient:
    """RabbitMQ connection manager with reconnection logic."""

    def __init__(self):
        self.connection = None
        self.channel = None
        self._connect()

    def _connect(self):
        """Establish connection to RabbitMQ with retry logic."""
        max_retries = 10
        retry_count = 0

        while retry_count < max_retries:
            try:
                credentials = pika.PlainCredentials(
                    Config.RABBITMQ_USER, Config.RABBITMQ_PASS
                )
                parameters = pika.ConnectionParameters(
                    host=Config.RABBITMQ_HOST,
                    port=Config.RABBITMQ_PORT,
                    credentials=credentials,
                    heartbeat=600,
                    blocked_connection_timeout=300,
                    client_properties={"connection_name": "PriceGenius-AI-Agent"},
                )

                self.connection = pika.BlockingConnection(parameters)
                self.channel = self.connection.channel()

                # Declare queues (idempotent)
                self.channel.queue_declare(
                    queue=Config.MARKET_ANALYSIS_QUEUE, durable=True
                )
                self.channel.queue_declare(
                    queue=Config.PRICE_UPDATE_QUEUE, durable=True
                )

                # Fair dispatch - process one message at a time
                self.channel.basic_qos(prefetch_count=1)

                logger.info("✅ RabbitMQ bağlantısı kuruldu.")
                return

            except Exception as e:
                retry_count += 1
                wait_time = min(retry_count * 2, 30)
                logger.warning(
                    f"⏳ RabbitMQ bağlantı denemesi {retry_count}/{max_retries} başarısız: {e}"
                )
                time.sleep(wait_time)

        raise ConnectionError("RabbitMQ'ya bağlanılamadı — maksimum deneme sayısı aşıldı.")

    def publish(self, queue_name: str, message: dict):
        """Publish a JSON message to the specified queue."""
        try:
            body = json.dumps(message, ensure_ascii=False)
            self.channel.basic_publish(
                exchange="",
                routing_key=queue_name,
                body=body.encode("utf-8"),
                properties=pika.BasicProperties(
                    delivery_mode=2,  # Persistent
                    content_type="application/json",
                ),
            )
            logger.info(f"📤 Mesaj gönderildi → {queue_name}: {body[:200]}")
        except Exception as e:
            logger.error(f"❌ Mesaj gönderme hatası: {e}")
            self._reconnect()
            self.publish(queue_name, message)

    def consume(self, queue_name: str, callback):
        """Start consuming messages from the specified queue."""
        try:
            self.channel.basic_consume(
                queue=queue_name,
                on_message_callback=callback,
                auto_ack=False,
            )
            logger.info(f"👂 Kuyruk dinleniyor: {queue_name}")
            self.channel.start_consuming()
        except KeyboardInterrupt:
            logger.info("🛑 Consumer durduruldu.")
            self.close()
        except Exception as e:
            logger.error(f"❌ Consumer hatası: {e}")
            self._reconnect()
            self.consume(queue_name, callback)

    def _reconnect(self):
        """Attempt to reconnect after a failure."""
        logger.warning("🔄 RabbitMQ yeniden bağlanılıyor...")
        try:
            if self.connection and not self.connection.is_closed:
                self.connection.close()
        except Exception:
            pass
        time.sleep(5)
        self._connect()

    def close(self):
        """Gracefully close the connection."""
        try:
            if self.channel and self.channel.is_open:
                self.channel.stop_consuming()
            if self.connection and not self.connection.is_closed:
                self.connection.close()
            logger.info("🔌 RabbitMQ bağlantısı kapatıldı.")
        except Exception as e:
            logger.warning(f"Bağlantı kapatma hatası: {e}")
