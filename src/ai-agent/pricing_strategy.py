import json
import re
import logging

logger = logging.getLogger(__name__)

# Valid strategy types
VALID_STRATEGIES = ["undercut", "premium", "match", "opportunistic", "hold"]


def parse_ai_response(response_text: str) -> dict | None:
    """
    Parse the AI response and extract the pricing decision.
    Expects JSON with: suggestedPrice, strategy, reasoning, confidenceScore
    """
    try:
        # Try to extract JSON from the response (handle markdown code blocks)
        json_match = re.search(r"\{[^{}]*\}", response_text, re.DOTALL)
        if not json_match:
            logger.error(f"❌ JSON bulunamadı AI yanıtında: {response_text[:300]}")
            return None

        data = json.loads(json_match.group())

        # Validate required fields
        suggested_price = data.get("suggestedPrice") or data.get("suggested_price")
        strategy = data.get("strategy", "hold")
        reasoning = data.get("reasoning", "Açıklama yok")
        confidence = data.get("confidenceScore") or data.get("confidence_score", 50)

        if suggested_price is None:
            logger.error("❌ suggestedPrice alanı eksik.")
            return None

        suggested_price = float(suggested_price)
        confidence = int(min(max(confidence, 0), 100))

        # Normalize strategy
        if strategy not in VALID_STRATEGIES:
            logger.warning(f"⚠️ Bilinmeyen strateji '{strategy}', 'hold' olarak ayarlandı.")
            strategy = "hold"

        return {
            "suggestedPrice": round(suggested_price, 2),
            "strategy": strategy,
            "reasoning": reasoning,
            "confidenceScore": confidence,
        }

    except json.JSONDecodeError as e:
        logger.error(f"❌ JSON parse hatası: {e}\nYanıt: {response_text[:500]}")
        return None
    except Exception as e:
        logger.error(f"❌ Yanıt işleme hatası: {e}")
        return None


def build_prompt(market_data: dict) -> str:
    """Build the structured prompt for Gemini."""

    competitors_text = ""
    for comp in market_data.get("competitors", []):
        stock_status = "STOK BİTTİ ❌" if comp.get("stockQuantity", 0) == 0 else f"Stok: {comp['stockQuantity']} adet"
        price_change = ""
        if "previousPrice" in comp and comp["previousPrice"] != comp["price"]:
            diff = comp["price"] - comp["previousPrice"]
            direction = "↑" if diff > 0 else "↓"
            price_change = f" ({direction} {abs(diff):.2f} TL değişim)"

        competitors_text += f"  - {comp['name']}: {comp['price']:.2f} TL {price_change} — {stock_status}\n"

    prompt = f"""Sen deneyimli bir e-ticaret fiyatlandırma uzmanısın. Aşağıdaki piyasa verisini analiz ederek optimal satış fiyatını belirle.

## Ürün Bilgisi
- **Ürün:** {market_data.get('productName', 'Bilinmiyor')}
- **Maliyet Fiyatı:** {market_data.get('costPrice', 0):.2f} TL
- **Mevcut Satış Fiyatı:** {market_data.get('currentPrice', 0):.2f} TL
- **Minimum Kar Marjı:** %{market_data.get('minProfitMargin', 15)}

## Piyasa Değişikliği
- **Değişiklik Türü:** {market_data.get('changeType', 'bilinmiyor')}

## Rakip Durumu
{competitors_text}

## Kurallar
1. Fiyat, maliyet fiyatının üzerinde olmalıdır (minimum kar marjını koruyarak).
2. Rakip stok bitirdiyse fırsat stratejisi (opportunistic) uygula — kar marjını artır.
3. Rakip fiyat düşürdüyse altından girme (undercut) veya eşleme (match) stratejisi uygula.
4. Rakip fiyat artırdıysa premium strateji uygulayabilirsin.
5. Güvenilirlik skoru %0-100 arasında olmalı.

## Yanıt Formatı
Aşağıdaki JSON formatında yanıt ver (sadece JSON, başka bir şey yazma):
{{
  "suggestedPrice": <sayı>,
  "strategy": "<undercut|premium|match|opportunistic|hold>",
  "reasoning": "<Türkçe kısa açıklama, maksimum 2 cümle>",
  "confidenceScore": <0-100>
}}"""

    return prompt
