import sys
import os
import pdfplumber
import pytesseract
from PIL import Image, ImageEnhance
import re
import unicodedata
import spacy
from groq import Groq
import json
import pyodbc

sys.stdout.reconfigure(encoding='utf-8')
api_key = os.getenv("GROQ_API_KEY")
client = Groq(api_key=api_key)


nlp = spacy.load("fr_core_news_sm")


# --------------------------------------------------
# OCR & EXTRACTION
# --------------------------------------------------

def extract_text(file_path):
    if not os.path.exists(file_path): return "Erreur: fichier introuvable"
    try:
        if file_path.lower().endswith(".pdf"):
            text = ""
            with pdfplumber.open(file_path) as pdf:
                for page in pdf.pages:
                    page_text = page.extract_text(x_tolerance=3, y_tolerance=3)
                    if page_text and len(page_text.strip()) > 20:
                        text += page_text + "\n"
                    
            return text
        
    except Exception as e: return f"Erreur: {str(e)}"

def fix_split_letters(text):
    def merge_word(match): return match.group(0).replace(" ", "")
    text = re.sub(r'\b(?:[A-Z]\s){2,}[A-Z]\b', merge_word, text)
    text = re.sub(r'\n{2,}', '\n', text)
    return text

def classify_with_groq(text):
    api_key = os.getenv("GROQ_API_KEY")
    if not api_key:
        return {"error": "GROQ_API_KEY non configurée"}

    try:
        client = Groq(api_key=api_key)
        system_message = {
            "role": "system",
            "content": (
                "You are a multilingual CV analysis expert. Extract structured candidate "
                "information from a resume or CV in any professional domain, including IT, "
                "finance, marketing, engineering, healthcare, education, law, logistics, and others. "
                "Do not assume any single industry."
            )
        }
        user_message = {
            "role": "user",
            "content": f"""
Analyse le CV suivant. Retourne UNIQUEMENT un JSON valide avec cette structure (pas de markdown, pas de texte additionnel) :
{{
  "competences": [],
  "experiences": [],
  "diplomes": [],
  "certifications": [],
  "languages": [],
  "projects": [],
  "soft_skills": [],
  "keywords": [],
  "confidence": 0.0
}}
Règles d'extraction :
- Extrait les informations selon le sens, pas uniquement des mots-clés.
- Fonctionne pour tous les domaines professionnels.
- Supporte le français, l'anglais et les CV mixtes.
- Sois robuste face aux CV non structurés ou incomplets.
- Normalise les données extraites.
- Détecte les synonymes et les équivalences métier.
- Inclut les stages, projets académiques et missions freelance comme expériences.
- Si une section est absente, retourne un tableau vide pour cette section.
- Confidence doit être un nombre entre 0.0 et 1.0.

CV:\n{text[:3800]}"""
        }

        response = client.chat.completions.create(
            model="llama-3.3-70b-versatile",
            messages=[system_message, user_message],
            temperature=0.0,
            max_tokens=1200,
        )

        raw = response.choices[0].message.content.strip()
        raw = re.sub(r'```json|```', '', raw).strip()
        data = parse_json_response(raw)

        if data is None or not isinstance(data, dict):
            return {"error": "Impossible de parser la réponse Groq", "raw": raw}

        return {
            "Competences": normalize_list(extract_list(data, "competences")),
            "Experiences": normalize_list(extract_list(data, "experiences")),
            "Diplomes": normalize_list(extract_list(data, "diplomes")),
            "Certifications": normalize_list(extract_list(data, "certifications")),
            "Languages": normalize_list(extract_list(data, "languages")),
            "Projects": normalize_list(extract_list(data, "projects")),
            "SoftSkills": normalize_list(extract_list(data, "soft_skills")),
            "Keywords": normalize_list(extract_list(data, "keywords")),
            "Confidence": extract_float(data, "confidence")
        }
    except Exception as e:
        return {"error": f"Erreur Groq: {str(e)}"}


def parse_json_response(raw):
    decoder = json.JSONDecoder()
    try:
        return decoder.decode(raw)
    except json.JSONDecodeError:
        start = raw.find('{')
        end = raw.rfind('}')
        if start != -1 and end != -1 and end > start:
            try:
                return decoder.decode(raw[start:end + 1])
            except json.JSONDecodeError:
                pass
    return None


def extract_list(data, key):
    value = data.get(key, [])
    if isinstance(value, str):
        return [normalize_text(value)]
    if isinstance(value, list):
        return [normalize_text(str(item)) for item in value if str(item).strip()]
    return []


def extract_float(data, key):
    if key not in data:
        return 0.0
    try:
        return float(data.get(key, 0.0))
    except (ValueError, TypeError):
        return 0.0


def normalize_text(value):
    if not value:
        return ""
    text = str(value).strip()
    text = unicodedata.normalize('NFKD', text)
    text = ''.join(ch for ch in text if unicodedata.category(ch) != 'Mn')
    text = re.sub(r'[\r\n\t]+', ' ', text)
    text = re.sub(r'\s+', ' ', text)
    text = text.strip(' .,-;:')
    return text


def normalize_list(items):
    seen = set()
    normalized = []
    for item in items:
        value = normalize_text(item)
        if not value:
            continue
        key = value.lower()
        if key in seen:
            continue
        seen.add(key)
        normalized.append(value)
    return normalized


def get_connection(conn_str):
    # Normalize .NET connection string for pyodbc
    conn_str = conn_str.replace("=True", "=yes").replace("=False", "=no")
    if "Driver=" not in conn_str:
        conn_str = "Driver={ODBC Driver 17 for SQL Server};" + conn_str
    return pyodbc.connect(conn_str)

if __name__ == "__main__":
    try:
        if len(sys.argv) < 4:
            print(json.dumps({"error": "Usage: python cv_extraction_api.py <fichier_pdf> <cv_id> <connection_string>"}))
            sys.exit(1)

        file_path = sys.argv[1]
        cv_id = sys.argv[2]
        conn_str = sys.argv[3]

        text = extract_text(file_path)

        if text.startswith("Erreur") or not text.strip():
            print(json.dumps({"error": f"Extraction de texte échouée: {text}"}))
            sys.exit(0)

        text = fix_split_letters(text)
        sections = classify_with_groq(text)
        
        if "error" in sections:
            print(json.dumps({"error": sections["error"]}))
            sys.exit(0)

        competences_str = ", ".join([s.strip() for s in sections.get("Competences", []) if s.strip()])
        experience_str = " | ".join([s.strip() for s in sections.get("Experiences", []) if s.strip()])
        diplomes_str = " | ".join([s.strip() for s in sections.get("Diplomes", []) if s.strip()])

        try:
            conn = get_connection(conn_str)
            cursor = conn.cursor()

            cursor.execute("SELECT Id FROM CvStructuredData WHERE CvId = ?", (cv_id,))
            structured_row = cursor.fetchone()
            if structured_row:
                cursor.execute(
                    "UPDATE CvStructuredData SET Competences = ?, Experiences = ?, Diplomes = ? WHERE CvId = ?",
                    (competences_str, experience_str, diplomes_str, cv_id)
                )
            else:
                cursor.execute(
                    "INSERT INTO CvStructuredData (CvId, Competences, Experiences, Diplomes) VALUES (?, ?, ?, ?)",
                    (cv_id, competences_str, experience_str, diplomes_str)
                )

            # Keep legacy Cv columns in sync for backwards compatibility.
            cursor.execute(
                "UPDATE Cv SET Competences = ?, Experience = ?, NiveauEducation = ? WHERE Id = ?",
                (competences_str, experience_str, diplomes_str, cv_id)
            )

            conn.commit()
            conn.close()
            
            print(json.dumps({"success": True, "cv_id": cv_id, "sections": sections}, ensure_ascii=False))
        except Exception as e:
            print(json.dumps({"error": f"Database error: {str(e)}"}))
            sys.exit(0)
    except Exception as global_e:
        print(json.dumps({"error": f"Global script error: {str(global_e)}"}))
        sys.exit(0)
