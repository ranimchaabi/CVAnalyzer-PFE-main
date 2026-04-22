import sys
import pdfplumber
import pytesseract
from PIL import Image, ImageEnhance
import os 
import re
import numpy as np
import spacy
from groq import Groq
import json
import pandas as pd

# -----------------------------------
# Config
# -----------------------------------
sys.stdout.reconfigure(encoding='utf-8')
import os
api_key = os.getenv("GROQ_API_KEY")

client = Groq(api_key=GROQ_API_KEY)
nlp = spacy.load("fr_core_news_sm")

# --------------------------------------------------
# OCR & EXTRACTION
# --------------------------------------------------
def ocr_image(img_input):
    """OCR amélioré"""
    if isinstance(img_input, str):
        img = Image.open(img_input)
    else:
        img = img_input.copy()

    if img.width < 2000:
        scale = 3000 / img.width
        img = img.resize((int(img.width * scale), int(img.height * scale)), Image.LANCZOS)

    results = []
    # Essai 1
    try:
        gray = img.convert('L')
        text = pytesseract.image_to_string(gray, lang="fra+eng", config=r'--oem 3 --psm 6')
        text = re.sub(r'[ \t]+', ' ', text).strip()
        text = re.sub(r'\n ', '\n', text)
        if len(text) > 10: results.append(text)
    except: pass

    # Essai 2: Contraste
    try:
        gray = img.convert('L')
        enhancer = ImageEnhance.Contrast(gray)
        gray = enhancer.enhance(2.0)
        enhancer = ImageEnhance.Sharpness(gray)
        gray = enhancer.enhance(2.0)
        text = pytesseract.image_to_string(gray, lang="fra+eng", config=r'--oem 3 --psm 6')
        text = re.sub(r'[ \t]+', ' ', text).strip()
        text = re.sub(r'\n ', '\n', text)
        if len(text) > 10: results.append(text)
    except: pass

    # Essai 3: PSM 3
    try:
        gray = img.convert('L')
        enhancer = ImageEnhance.Contrast(gray)
        gray = enhancer.enhance(1.
        text = pytesseract.image_to_string(gray, lang="fra+eng", config=r'--oem 3 --psm 3')
        text = re.sub(r'[ \t]+', ' ', text).strip()
        text = re.sub(r'\n ', '\n', text)
        if len(text) > 10: results.append(text)
    except: pass

    # Essai 4: PSM 4
    try:
        gray = img.convert('L')
        enhancer = ImageEnhance.Contrast(gray)
        gray = enhancer.enhance(1.5)
        text = pytesseract.image_to_string(gray, lang="fra+eng", config=r'--oem 3 --psm 4')
        text = re.sub(r'[ \t]+', ' ', text).strip()
        text = re.sub(r'\n ', '\n', text)
        if len(text) > 10: results.append(text)
    except: pass

    # Essai 5: PSM 11
    try:
        gray = img.convert('L')
        enhancer = ImageEnhance.Contrast(gray)
        gray = enhancer.enhance(2.0)
        text = pytesseract.image_to_string(gray, lang="fra+eng", config=r'--oem 3 --psm 11')
        text = re.sub(r'[ \t]+', ' ', text).strip()
        text = re.sub(r'\n ', '\n', text)
        if len(text) > 10: results.append(text)
    except: pass

    if results: return max(results, key=len)
    return ""

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
                    else:
                        img = page.to_image(resolution=400).original
                        text += ocr_image(img) + "\n"
            return text
        else: return ocr_image(Image.open(file_path))
    except Exception as e: return f"Erreur: {str(e)}"

def fix_split_letters(text):
    def merge_word(match): return match.group(0).replace(" ", "")
    text = re.sub(r'\b(?:[A-Z]\s){2,}[A-Z]\b', merge_word, text)
    text = re.sub(r'\n{2,}', '\n', text)
    return text

def get_nom(text):
    try:
        lines = [l.strip() for l in text.split('\n') if l.strip()]
        header_text = '\n'.join(lines[:10])
        response = client.chat.completions.create(
            model="llama-3.3-70b-versatile",
            messages=[{
                "role": "system",
                "content": "Tu extrais le nom COMPLET depuis un CV. Retourne UNIQUEMENT le nom."
            }, {"role": "user", "content": f"Nom:\n\n{header_text}"}],
            temperature=0, max_tokens=50,
        )
        result = response.choices[0].message.content.strip()
        result = re.sub(r'^(nom\s*:?\s*)', '', result, flags=re.IGNORECASE).strip(' .:;,-')
        if '@' in result or re.search(r'\d{6,}', result): return "Non trouvé"
        if len(result) > 50 or len(result) < 2: return "Non trouvé"
        return result if result else "Non trouvé"
    except: return "Non trouvé"

def get_email(text):
    match = re.search(r'[\w\.-]+@[\w\.-]+\.\w+', text)
    return match.group(0) if match else "Non trouvé"

def get_tel(text):
    patterns = [
        r'\+?\d{1,4}[\s\.\-]?\(?\d{1,4}\)?[\s\.\-]?\d{1,4}[\s\.\-]?\d{1,4}[\s\.\-]?\d{0,4}',
        r'\(\+\d{1,4}\)\s?\d[\d\s\-\.]{6,14}', r'0\d[\d\s\-\.]{7,12}\d', r'\(\d{3}\)\s?\d{3}[\-\s]?\d{4}'
    ]
    for p in patterns:
        matches = re.finditer(p, text)
        for match in matches:
            phone = match.group(0).strip()
            phone = re.sub(r'[\s\-\.]+$', '', phone).replace(r'[\s\-\.]{2,}', ' ')
            digits = re.sub(r'\D', '', phone)
            if 7 <= len(digits) <= 15:
                if phone.startswith('+') or re.search(r'[\s\.\-]', phone): return phone
                if phone.startswith('0') and 8 <= len(digits) <= 14: return phone
    return "Non trouvé"

def classify_with_groq(text):
    prompt = f"""
Tu es un expert en analyse de CV. Analyse le CV suivant et extrait les informations.
Retourne UNIQUEMENT un JSON valide avec cette structure (rien d'autre, pas de markdown):
{{
  "competences": ["liste des compétences techniques, langages, frameworks, outils"],
  "experiences": ["liste des expériences professionnelles, stages, projets, projets académiques"],
  "diplomes": ["liste des diplômes, formations, certifications"]
}}
CV: {text[:4000]}"""
    try:
        response = client.chat.completions.create(
            model="llama-3.3-70b-versatile",
            messages=[{"role": "user", "content": prompt}],
            temperature=0.1, max_tokens=1500,
        )
        raw = re.sub(r'```json|```', '', response.choices[0].message.content.strip()).strip()
        data = json.loads(raw)
        return {
            "Competences": data.get("competences", [])[:15],
            "Experiences": data.get("experiences", [])[:15],
            "Diplomes": data.get("diplomes", [])[:15]
        }
    except Exception as e:
        print(f"Erreur Groq: {e}")
        return {"Competences": [], "Experiences": [], "Diplomes": []}

# --------------------------------------------------
# FONCTION SCORE AVEC PANDAS (Sur Texte Brut)
# --------------------------------------------------
def calculate_match_score(job_description, full_cv_text):
    """
    Calcule un score de matching (0-100) entre le Job et le TEXTE COMPLET du CV.
    """
    if not job_description or not job_description.strip():
        return 0, []

    # 1. Nettoyage et extraction des mots-clés du Job Description (via Spacy)
    doc_job = nlp(job_description.lower())
    job_keywords = set([
        token.lemma_ for token in doc_job 
        if not token.is_stop and token.pos_ in ['NOUN', 'ADJ', 'VERB', 'PROPN'] and len(token.text) > 2
    ])

    if not job_keywords:
        return 0, []

    # 2. Préparation du texte CV
    cv_text_lower = full_cv_text.lower()

    # 3. Utilisation de PANDAS
    df_keywords = pd.DataFrame(list(job_keywords), columns=['keyword'])
    df_keywords['found_in_cv'] = df_keywords['keyword'].apply(lambda kw: kw in cv_text_lower)
    
    # 4. Calcul du score
    total_keywords = len(df_keywords)
    found_keywords_count = df_keywords['found_in_cv'].sum()
    score = int((found_keywords_count / total_keywords) * 100) if total_keywords > 0 else 0
    
    found_list = df_keywords[df_keywords['found_in_cv']]['keyword'].tolist()
    
    return score, found_list

# --------------------------------------------------
# MAIN (CORRIGÉ)
# --------------------------------------------------
if _name_ == "_main_":
    # --- CORRECTION ICI : Récupération des arguments ---
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: python script.py <fichier> [job_description]"}))
        sys.exit(1)

    file_path = sys.argv[1] # Définition de file_path
    job_desc = sys.argv[2] if len(sys.argv) > 2 else "" # Définition de job_desc
    # --------------------------------------------------

    text = extract_text(file_path)
    
    if text.startswith("Erreur") or not text.strip():
        print(json.dumps({"error": text}))
        sys.exit(0)

    text = fix_split_letters(text)

    nom      = get_nom(text)
    email    = get_email(text)
    tel      = get_tel(text)
    sections = classify_with_groq(text)

    # --- CALCUL DU SCORE SUR TEXTE BRUT ---
    score, matched_keywords = calculate_match_score(job_desc, text)

    output_data = {
        "nom": nom,
        "email": email,
        "tel": tel,
        "sections": sections,
        "score": score,
        "matched_keywords": matched_keywords
    }

    print(json.dumps(output_data, ensure_ascii=False))