"""
PriceGenius AI Agent — Worker Service
=====================================
RabbitMQ'dan market_analysis_queue'yu dinler, Gemini AI ile analiz yapar,
sonuçları price_update_queue'ya gönderir.
"""

import json
import time
import signal
import sys
import logging
from datetime import datetime, timezone

from config import Config
from rabbitmq_client import RabbitMQClient
from agent import PricingAgent

# --- Logging Setup ---
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s │ %(levelname)-8s │ %(name)-20s │ %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
    handlers=[logging.StreamHandler(sys.stdout)],
)
logger = logging.getLogger("PriceGenius.Agent")


def main():
    logger.info("=" * 60)
    logger.info("🚀 PriceGenius AI Agent başlatılıyor...")
    logger.info(f"   RabbitMQ: {Config.RABBITMQ_HOST}:{Config.RABBITMQ_PORT}")
    logger.info(f"   Gemini Model: {Config.GEMINI_MODEL}")
    logger.info(f"   İşlem Gecikmesi: {Config.PROCESSING_DELAY}s")
    logger.info("=" * 60)

    # Initialize components
    rabbitmq = RabbitMQClient()
    agent = PricingAgent()

    # Graceful shutdown handler
    def shutdown_handler(signum, frame):
        logger.info("🛑 Kapatma sinyali alındı. Bağlantılar kapatılıyor...")
        rabbitmq.close()
        sys.exit(0)

    signal.signal(signal.SIGINT, shutdown_handler)
    signal.signal(signal.SIGTERM, shutdown_handler)

    # Message callback
    def on_market_analysis(ch, method, properties, body):
        try:
            message = json.loads(body.decode("utf-8"))
            product_name = message.get("productName", "Bilinmiyor")
            product_id = message.get("productId", "?")

            logger.info(f"📥 Mesaj alındı — Ürün: {product_name} (ID: {product_id})")

            # AI Analysis
            decision = agent.analyze(message)

            if decision:
                # Add timestamp
                decision["timestamp"] = datetime.now(timezone.utc).isoformat()

                # Publish to price_update_queue
                rabbitmq.publish(Config.PRICE_UPDATE_QUEUE, decision)

                logger.info(
                    f"✅ Karar gönderildi: {product_name} → "
                    f"{decision['suggestedPrice']} TL ({decision['strategy']})"
                )
            else:
                logger.warning(f"⚠️ Karar üretilemedi: {product_name}")

            # Acknowledge the message
            ch.basic_ack(delivery_tag=method.delivery_tag)

            # Throttling — respect Gemini API rate limits
            time.sleep(Config.PROCESSING_DELAY)

        except json.JSONDecodeError as e:
            logger.error(f"❌ JSON parse hatası: {e}")
            ch.basic_nack(delivery_tag=method.delivery_tag, requeue=False)

        except Exception as e:
            logger.error(f"❌ İşlem hatası: {e}")
            ch.basic_nack(delivery_tag=method.delivery_tag, requeue=True)

    # Start consuming
    logger.info(f"👂 Kuyruk dinleniyor: {Config.MARKET_ANALYSIS_QUEUE}")
    logger.info("   Mesaj bekleniyor... (Ctrl+C ile durdurulabilir)")
    logger.info("-" * 60)

    rabbitmq.consume(Config.MARKET_ANALYSIS_QUEUE, on_market_analysis)


if __name__ == "__main__":
    main()
