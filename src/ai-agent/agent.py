import logging
import google.generativeai as genai
from config import Config
from pricing_strategy import build_prompt, parse_ai_response

logger = logging.getLogger(__name__)


class PricingAgent:
    """AI Agent that analyzes market data and suggests optimal pricing using Gemini."""

    def __init__(self):
        if not Config.GEMINI_API_KEY:
            logger.warning("⚠️ GEMINI_API_KEY ayarlanmamış. Fallback stratejisi kullanılacak.")
            self.model = None
        else:
            genai.configure(api_key=Config.GEMINI_API_KEY)
            self.model = genai.GenerativeModel(
                model_name=Config.GEMINI_MODEL,
                generation_config={
                    "temperature": 0.3,
                    "top_p": 0.8,
                    "max_output_tokens": 500,
                },
            )
            logger.info(f"🤖 Gemini AI Agent başlatıldı (Model: {Config.GEMINI_MODEL})")

    def analyze(self, market_data: dict) -> dict | None:
        """
        Analyze market data and return a pricing decision.
        Falls back to rule-based strategy if Gemini is unavailable.
        """
        product_name = market_data.get("productName", "Bilinmiyor")
        logger.info(f"🧠 Analiz başlatılıyor: {product_name}")

        # Try AI-powered analysis
        if self.model:
            try:
                prompt = build_prompt(market_data)
                response = self.model.generate_content(prompt)

                if response and response.text:
                    logger.info(f"📝 Gemini yanıtı alındı: {response.text[:200]}")
                    result = parse_ai_response(response.text)
                    if result:
                        result["productId"] = market_data.get("productId")
                        logger.info(
                            f"✅ AI Karar: {product_name} → {result['suggestedPrice']} TL "
                            f"(Strateji: {result['strategy']}, Güven: %{result['confidenceScore']})"
                        )
                        return result
                    else:
                        logger.warning("⚠️ AI yanıtı parse edilemedi, fallback kullanılıyor.")
                else:
                    logger.warning("⚠️ Gemini boş yanıt döndü, fallback kullanılıyor.")

            except Exception as e:
                logger.error(f"❌ Gemini API hatası: {e}")

        # Fallback: Rule-based strategy
        return self._fallback_strategy(market_data)

    def _fallback_strategy(self, market_data: dict) -> dict:
        """Simple rule-based fallback when Gemini is unavailable."""
        product_id = market_data.get("productId")
        product_name = market_data.get("productName", "Bilinmiyor")
        cost_price = market_data.get("costPrice", 0)
        current_price = market_data.get("currentPrice", 0)
        change_type = market_data.get("changeType", "unknown")
        min_margin = market_data.get("minProfitMargin", 15)
        competitors = market_data.get("competitors", [])

        # Calculate average competitor price
        active_competitors = [c for c in competitors if c.get("stockQuantity", 0) > 0]
        out_of_stock = [c for c in competitors if c.get("stockQuantity", 0) == 0]

        if active_competitors:
            avg_competitor_price = sum(c["price"] for c in active_competitors) / len(active_competitors)
        else:
            avg_competitor_price = current_price

        min_allowed = cost_price * (1 + min_margin / 100)

        if change_type == "stock_out" and len(out_of_stock) > 0:
            # Opportunity: competitors out of stock → increase price
            suggested = current_price * 1.15
            strategy = "opportunistic"
            reasoning = f"Rakiplerden {len(out_of_stock)} tanesi stok bitirdi. Fiyat %15 artırılarak fırsat değerlendiriliyor."
            confidence = 75
        elif change_type == "price_drop":
            # Competitor dropped price → match or undercut
            suggested = avg_competitor_price * 0.98
            strategy = "undercut"
            reasoning = f"Rakipler fiyat düşürdü. Ortalama rakip fiyatının %2 altına inilerek rekabet sağlanıyor."
            confidence = 65
        elif change_type == "price_increase":
            # Competitor increased price → premium opportunity
            suggested = current_price * 1.08
            strategy = "premium"
            reasoning = f"Rakipler fiyat artırdı. Mevcut fiyat %8 artırılarak premium konumlandırma yapılıyor."
            confidence = 60
        else:
            suggested = current_price
            strategy = "hold"
            reasoning = "Piyasada anlamlı bir değişiklik yok. Mevcut fiyat korunuyor."
            confidence = 50

        # Ensure minimum margin
        suggested = max(suggested, min_allowed)

        result = {
            "productId": product_id,
            "suggestedPrice": round(suggested, 2),
            "strategy": strategy,
            "reasoning": f"[Fallback] {reasoning}",
            "confidenceScore": confidence,
        }

        logger.info(
            f"🔧 Fallback Karar: {product_name} → {result['suggestedPrice']} TL "
            f"(Strateji: {strategy})"
        )
        return result
