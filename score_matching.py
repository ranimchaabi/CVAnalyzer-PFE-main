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

client = Groq(api_key=api_key)
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
def _extract_required_years(job_description):
    years = re.findall(r'(\d+)\s*(?:\+?\s*)?(?:ans?|années?)', (job_description or "").lower())
    if not years:
        return 0
    return max(int(y) for y in years)

def _parse_job_requirements(job_description):
    """
    Accepte soit:
    - un texte libre (mode actuel)
    - un JSON formulaire:
      {"required_diplomas":[...], "required_skills":[...], "required_experience_years":N}
    """
    req = {
        "required_diplomas": [],
        "required_skills": [],
        "required_experience_years": 0,
        "raw_text": job_description or ""
    }

    raw = (job_description or "").strip()
    if not raw:
        return req

    try:
        parsed = json.loads(raw)
        if isinstance(parsed, dict):
            dipl = parsed.get("required_diplomas", [])
            skills = parsed.get("required_skills", [])
            years = parsed.get("required_experience_years", 0)

            req["required_diplomas"] = dipl if isinstance(dipl, list) else [dipl]
            req["required_skills"] = skills if isinstance(skills, list) else [skills]
            req["required_experience_years"] = int(years) if str(years).isdigit() else 0
            req["raw_text"] = parsed.get("job_description", raw)
            return req
    except Exception:
        pass

    return req

def _extract_required_diploma(job_description):
    text = (job_description or "").lower()
    if re.search(r'phd|doctorat|doctorate', text):
        return "phd"
    if re.search(r'master|mastère|bac\+5|ing[ée]nieur', text):
        return "master"
    if re.search(r'bachelor|licence|bac\+3', text):
        return "bachelor"
    return ""

def _diploma_rank(value):
    v = (value or "").lower()
    if re.search(r'phd|doctorat|doctorate', v):
        return 3
    if re.search(r'master|mastère|bac\+5|ing[ée]nieur', v):
        return 2
    if re.search(r'bachelor|licence|bac\+3', v):
        return 1
    return 0

def _extract_years_from_text(value):
    if value is None:
        return 0
    nums = re.findall(r'(\d+)', str(value).lower())
    if not nums:
        return 0
    return max(int(n) for n in nums)

def _normalize_text(v):
    return re.sub(r'\s+', ' ', str(v or "").lower()).strip()

def _skill_partial_match(req_skill, cv_skill):
    req_tokens = set(re.findall(r'[a-z0-9\+\#\.]+', _normalize_text(req_skill)))
    cv_tokens = set(re.findall(r'[a-z0-9\+\#\.]+', _normalize_text(cv_skill)))
    if not req_tokens or not cv_tokens:
        return False
    overlap = len(req_tokens.intersection(cv_tokens))
    ratio = overlap / max(len(req_tokens), 1)
    return ratio >= 0.6

def score_skills(cv_skills, required_skills):
    cv_df = pd.DataFrame(cv_skills if isinstance(cv_skills, list) else [cv_skills], columns=["skill"])
    req_df = pd.DataFrame(required_skills if isinstance(required_skills, list) else [required_skills], columns=["req_skill"])

    cv_df = cv_df[cv_df["skill"].notna()]
    req_df = req_df[req_df["req_skill"].notna()]
    cv_df["skill"] = cv_df["skill"].astype(str).str.lower().str.strip()
    req_df["req_skill"] = req_df["req_skill"].astype(str).str.lower().str.strip()
    cv_df = cv_df[cv_df["skill"] != ""]
    req_df = req_df[req_df["req_skill"] != ""]

    if len(req_df) == 0:
        return 0
    if len(cv_df) == 0:
        return 0

    req_df["exact_match"] = req_df["req_skill"].isin(cv_df["skill"])
    req_df["partial_match"] = req_df["req_skill"].apply(
        lambda req: any(_skill_partial_match(req, cv_s) for cv_s in cv_df["skill"])
    )
    req_df["matched"] = req_df["exact_match"] | req_df["partial_match"]

    matched_count = req_df["matched"].sum()
    return int((matched_count / len(req_df)) * 100)

def score_experience(cv_experiences, required_years):
    exp_df = pd.DataFrame(cv_experiences if isinstance(cv_experiences, list) else [cv_experiences], columns=["experience"])
    exp_df = exp_df[exp_df["experience"].notna()]
    exp_df["experience"] = exp_df["experience"].astype(str).str.strip()
    exp_df = exp_df[exp_df["experience"] != ""]
    exp_df["years"] = exp_df["experience"].apply(_extract_years_from_text)

    total_years = exp_df["years"].sum() if len(exp_df) > 0 else 0
    _ = exp_df["years"].mean() if len(exp_df) > 0 else 0

    if required_years <= 0:
        return 100 if total_years > 0 else 0
    return int(min((total_years / required_years) * 100, 100))

def score_diplomas(cv_diplomas, required_diplomas):
    cv_df = pd.DataFrame(cv_diplomas if isinstance(cv_diplomas, list) else [cv_diplomas], columns=["diploma"])
    req_df = pd.DataFrame(required_diplomas if isinstance(required_diplomas, list) else [required_diplomas], columns=["required"])

    cv_df = cv_df[cv_df["diploma"].notna()]
    req_df = req_df[req_df["required"].notna()]
    cv_df["diploma"] = cv_df["diploma"].astype(str).str.strip()
    req_df["required"] = req_df["required"].astype(str).str.strip()
    cv_df = cv_df[cv_df["diploma"] != ""]
    req_df = req_df[req_df["required"] != ""]

    cv_df["rank"] = cv_df["diploma"].apply(_diploma_rank)
    req_df["rank"] = req_df["required"].apply(_diploma_rank)

    cv_best_rank = int(cv_df["rank"].max()) if len(cv_df) > 0 else 0
    required_rank = int(req_df["rank"].max()) if len(req_df) > 0 else 0

    if required_rank <= 0:
        return 100 if cv_best_rank > 0 else 0
    return int(min((cv_best_rank / required_rank) * 100, 100))

def calculate_match_score(job_description, full_cv_text, sections=None):
    """
    Calcule un score de matching (0-100) entre le Job et le TEXTE COMPLET du CV.
    """
    if not job_description or not job_description.strip():
        return 0, []

    requirements = _parse_job_requirements(job_description)
    job_text = requirements["raw_text"] or (job_description or "")

    # 1. Nettoyage et extraction des mots-clés du Job Description (via Spacy)
    doc_job = nlp(job_text.lower())
    job_keywords = set([
        token.lemma_ for token in doc_job 
        if not token.is_stop and token.pos_ in ['NOUN', 'ADJ', 'VERB', 'PROPN'] and len(token.text) > 2
    ])

    if not job_keywords:
        return 0, []

    # 2. Préparation du texte CV
    cv_text_lower = full_cv_text.lower()
    sections = sections or {}

    # 3. Utilisation de PANDAS (logique existante conservée)
    df_keywords = pd.DataFrame(list(job_keywords), columns=['keyword'])
    df_keywords['found_in_cv'] = df_keywords['keyword'].apply(lambda kw: kw in cv_text_lower)
    
    # 4. Calcul du score global keyword (logique existante)
    total_keywords = len(df_keywords)
    found_keywords_count = df_keywords['found_in_cv'].sum()
    keyword_score = int((found_keywords_count / total_keywords) * 100) if total_keywords > 0 else 0
    
    found_list = df_keywords[df_keywords['found_in_cv']]['keyword'].tolist()

    # 5. Sous-score Skills via API data + Pandas (.isin, .sum, filtering)
    cv_skills_raw = sections.get("Competences", []) if isinstance(sections, dict) else []
    required_skills = requirements["required_skills"] if requirements["required_skills"] else list(job_keywords)
    skills_score = score_skills(cv_skills_raw, required_skills)
    if skills_score == 0 and keyword_score > 0:
        skills_score = keyword_score

    # 6. Sous-score Experience via API data + Pandas (.sum, .mean, filtering)
    required_years = requirements["required_experience_years"] if requirements["required_experience_years"] > 0 else _extract_required_years(job_text)
    cv_exp_raw = sections.get("Experiences", []) if isinstance(sections, dict) else []
    exp_score = score_experience(cv_exp_raw, required_years)

    # 7. Sous-score Diploma via API data + Pandas (.max)
    required_diploma = _extract_required_diploma(job_text)
    cv_dipl_raw = sections.get("Diplomes", []) if isinstance(sections, dict) else []
    required_diplomas = requirements["required_diplomas"] if requirements["required_diplomas"] else ([required_diploma] if required_diploma else [])
    dipl_score = score_diplomas(cv_dipl_raw, required_diplomas)

    # 8. Score global amélioré (pondéré) en gardant le flux existant
    score = int(round((skills_score * 0.5) + (dipl_score * 0.2) + (exp_score * 0.3)))
    score = int(np.clip(score, 0, 100))
    
    return score, found_list

def calculate_match_score_details(job_description, full_cv_text, sections=None):
    if not job_description or not job_description.strip():
        return {"skills": 0, "diplomas": 0, "experience": 0}

    requirements = _parse_job_requirements(job_description)
    job_text = requirements["raw_text"] or (job_description or "")

    doc_job = nlp(job_text.lower())
    job_keywords = set([
        token.lemma_ for token in doc_job
        if not token.is_stop and token.pos_ in ['NOUN', 'ADJ', 'VERB', 'PROPN'] and len(token.text) > 2
    ])
    sections = sections or {}

    required_skills = requirements["required_skills"] if requirements["required_skills"] else list(job_keywords)
    required_years = requirements["required_experience_years"] if requirements["required_experience_years"] > 0 else _extract_required_years(job_text)
    required_diploma = _extract_required_diploma(job_text)
    required_diplomas = requirements["required_diplomas"] if requirements["required_diplomas"] else ([required_diploma] if required_diploma else [])

    skills = score_skills(sections.get("Competences", []), required_skills)
    experience = score_experience(sections.get("Experiences", []), required_years)
    diplomas = score_diplomas(sections.get("Diplomes", []), required_diplomas)
    return {"skills": int(skills), "diplomas": int(diplomas), "experience": int(experience)}

# --------------------------------------------------
# MAIN (CORRIGÉ)
# --------------------------------------------------
if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: python script.py <fichier> [job_description]"}))
        sys.exit(1)

    file_path = sys.argv[1]
    job_desc = sys.argv[2] if len(sys.argv) > 2 else ""

    text = extract_text(file_path)

    if text.startswith("Erreur") or not text.strip():
        print(json.dumps({"error": text}))
        sys.exit(0)

    text = fix_split_letters(text)

    sections = classify_with_groq(text)

    score, matched_keywords = calculate_match_score(job_desc, text, sections)
    score_details = calculate_match_score_details(job_desc, text, sections)

    output_data = {
        "sections": sections,
        "score": score,
        "details": score_details,
        "matched_keywords": matched_keywords
    }

    print(json.dumps(output_data, ensure_ascii=False))