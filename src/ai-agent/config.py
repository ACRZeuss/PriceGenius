import os
from dotenv import load_dotenv

load_dotenv()


class Config:
    # RabbitMQ
    RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "localhost")
    RABBITMQ_PORT = int(os.getenv("RABBITMQ_PORT", "5672"))
    RABBITMQ_USER = os.getenv("RABBITMQ_USER", "admin")
    RABBITMQ_PASS = os.getenv("RABBITMQ_PASS", "admin123")

    # Queues
    MARKET_ANALYSIS_QUEUE = "market_analysis_queue"
    PRICE_UPDATE_QUEUE = "price_update_queue"

    # Gemini
    GEMINI_API_KEY = os.getenv("GEMINI_API_KEY", "")
    GEMINI_MODEL = os.getenv("GEMINI_MODEL", "gemini-2.0-flash")

    # Throttling
    PROCESSING_DELAY = int(os.getenv("PROCESSING_DELAY", "2"))
