// Languages the built-in screens ship with. `name` is how the language calls itself (shown in the picker).
// Add a language: add it here and add locales/<code>.json (English text -> translation; missing keys fall back to English).
export interface Language { code: string; name: string; english: string; dir?: "rtl" }

export const LANGUAGES: Language[] = [
  { code: "en", name: "English", english: "English" },
  { code: "fr", name: "Français", english: "French" },
  { code: "es", name: "Español", english: "Spanish" },
  { code: "pt", name: "Português", english: "Portuguese" },
  { code: "de", name: "Deutsch", english: "German" },
  { code: "it", name: "Italiano", english: "Italian" },
  { code: "nl", name: "Nederlands", english: "Dutch" },
  { code: "pl", name: "Polski", english: "Polish" },
  { code: "ro", name: "Română", english: "Romanian" },
  { code: "cs", name: "Čeština", english: "Czech" },
  { code: "hu", name: "Magyar", english: "Hungarian" },
  { code: "el", name: "Ελληνικά", english: "Greek" },
  { code: "sv", name: "Svenska", english: "Swedish" },
  { code: "da", name: "Dansk", english: "Danish" },
  { code: "nb", name: "Norsk bokmål", english: "Norwegian" },
  { code: "fi", name: "Suomi", english: "Finnish" },
  { code: "tr", name: "Türkçe", english: "Turkish" },
  { code: "ru", name: "Русский", english: "Russian" },
  { code: "uk", name: "Українська", english: "Ukrainian" },
  { code: "ar", name: "العربية", english: "Arabic", dir: "rtl" },
  { code: "he", name: "עברית", english: "Hebrew", dir: "rtl" },
  { code: "fa", name: "فارسی", english: "Persian", dir: "rtl" },
  { code: "ur", name: "اردو", english: "Urdu", dir: "rtl" },
  { code: "hi", name: "हिन्दी", english: "Hindi" },
  { code: "gu", name: "ગુજરાતી", english: "Gujarati" },
  { code: "mr", name: "मराठी", english: "Marathi" },
  { code: "bn", name: "বাংলা", english: "Bengali" },
  { code: "pa", name: "ਪੰਜਾਬੀ", english: "Punjabi" },
  { code: "ta", name: "தமிழ்", english: "Tamil" },
  { code: "te", name: "తెలుగు", english: "Telugu" },
  { code: "kn", name: "ಕನ್ನಡ", english: "Kannada" },
  { code: "ml", name: "മലയാളം", english: "Malayalam" },
  { code: "zh-CN", name: "简体中文", english: "Chinese (Simplified)" },
  { code: "zh-TW", name: "繁體中文", english: "Chinese (Traditional)" },
  { code: "ja", name: "日本語", english: "Japanese" },
  { code: "ko", name: "한국어", english: "Korean" },
  { code: "vi", name: "Tiếng Việt", english: "Vietnamese" },
  { code: "th", name: "ไทย", english: "Thai" },
  { code: "id", name: "Bahasa Indonesia", english: "Indonesian" },
  { code: "ms", name: "Bahasa Melayu", english: "Malay" },
  { code: "fil", name: "Filipino", english: "Filipino" },
  { code: "sw", name: "Kiswahili", english: "Swahili" },
];

/** The supported language closest to a requested one ("fr-CA" -> "fr", "zh-HK" -> "zh-TW", "iw" -> "he"). */
export function matchLanguage(requested: string | null | undefined, available: string[] = LANGUAGES.map((l) => l.code)): string | null {
  if (!requested) return null;
  const r = requested.trim().replace("_", "-");
  const lower = r.toLowerCase();
  const exact = available.find((a) => a.toLowerCase() === lower);
  if (exact) return exact;
  const base = lower.split("-")[0];
  const alias: Record<string, string> = { iw: "he", no: "nb", nn: "nb", tl: "fil", in: "id" };
  if (base === "zh") {
    const trad = /-(tw|hk|mo|hant)/.test(lower);
    return available.find((a) => a === (trad ? "zh-TW" : "zh-CN")) || available.find((a) => a.startsWith("zh")) || null;
  }
  const b = alias[base] || base;
  return available.find((a) => a.toLowerCase() === b) || available.find((a) => a.toLowerCase().split("-")[0] === b) || null;
}
