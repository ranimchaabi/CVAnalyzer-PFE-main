import sys
import re
import numpy as np
import spacy
import json
import pandas as pd
import pyodbc

try:
    nlp = spacy.load("fr_core_news_sm")
except:
    pass

# --------------------------------------------------
# FONCTION SCORE AVEC PANDAS (Sur Texte Brut)
# --------------------------------------------------
def _extract_required_years(job_description):
    years = re.findall(r'(\d+)\s*(?:\+?\s*)?(?:ans?|années?)', (job_description or "").lower())
    if not years:
        return 0
    return max(int(y) for y in years)

def _parse_job_requirements(job_description):
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

def calculate_match_score(job_description, cv_text_for_keywords, sections=None):
    if not job_description or not job_description.strip():
        return 0, []

    requirements = _parse_job_requirements(job_description)
    job_text = requirements["raw_text"] or (job_description or "")

    doc_job = nlp(job_text.lower())
    job_keywords = set([
        token.lemma_ for token in doc_job 
        if not token.is_stop and token.pos_ in ['NOUN', 'ADJ', 'VERB', 'PROPN'] and len(token.text) > 2
    ])

    if not job_keywords:
        return 0, []

    cv_text_lower = cv_text_for_keywords.lower()
    sections = sections or {}

    df_keywords = pd.DataFrame(list(job_keywords), columns=['keyword'])
    df_keywords['found_in_cv'] = df_keywords['keyword'].apply(lambda kw: kw in cv_text_lower)
    
    total_keywords = len(df_keywords)
    found_keywords_count = df_keywords['found_in_cv'].sum()
    keyword_score = int((found_keywords_count / total_keywords) * 100) if total_keywords > 0 else 0
    
    found_list = df_keywords[df_keywords['found_in_cv']]['keyword'].tolist()

    cv_skills_raw = sections.get("Competences", []) if isinstance(sections, dict) else []
    required_skills = requirements["required_skills"] if requirements["required_skills"] else list(job_keywords)
    skills_score = score_skills(cv_skills_raw, required_skills)
    if skills_score == 0 and keyword_score > 0:
        skills_score = keyword_score

    required_years = requirements["required_experience_years"] if requirements["required_experience_years"] > 0 else _extract_required_years(job_text)
    cv_exp_raw = sections.get("Experiences", []) if isinstance(sections, dict) else []
    exp_score = score_experience(cv_exp_raw, required_years)

    required_diploma = _extract_required_diploma(job_text)
    cv_dipl_raw = sections.get("Diplomes", []) if isinstance(sections, dict) else []
    required_diplomas = requirements["required_diplomas"] if requirements["required_diplomas"] else ([required_diploma] if required_diploma else [])
    dipl_score = score_diplomas(cv_dipl_raw, required_diplomas)

    score = int(round((skills_score * 0.5) + (dipl_score * 0.2) + (exp_score * 0.3)))

    bonus = 0

    exp_df = pd.DataFrame(cv_exp_raw if isinstance(cv_exp_raw, list) else [cv_exp_raw], columns=["experience"])
    exp_df = exp_df[exp_df["experience"].notna()]
    exp_df["experience"] = exp_df["experience"].astype(str).str.strip()
    exp_df = exp_df[exp_df["experience"] != ""]
    cv_years = exp_df["experience"].apply(_extract_years_from_text).sum() if len(exp_df) > 0 else 0

    req_df = pd.DataFrame(required_diplomas if isinstance(required_diplomas, list) else [required_diplomas], columns=["required"])
    req_df = req_df[req_df["required"].notna()]
    req_df["required"] = req_df["required"].astype(str).str.strip()
    req_df = req_df[req_df["required"] != ""]
    required_rank = int(req_df["required"].apply(_diploma_rank).max()) if len(req_df) > 0 else 0

    cv_df = pd.DataFrame(cv_dipl_raw if isinstance(cv_dipl_raw, list) else [cv_dipl_raw], columns=["diploma"])
    cv_df = cv_df[cv_df["diploma"].notna()]
    cv_df["diploma"] = cv_df["diploma"].astype(str).str.strip()
    cv_df = cv_df[cv_df["diploma"] != ""]
    cv_best_rank = int(cv_df["diploma"].apply(_diploma_rank).max()) if len(cv_df) > 0 else 0

    if required_rank > 0 and cv_best_rank > required_rank:
        bonus += (cv_best_rank - required_rank)

    if cv_years <= 1 and cv_best_rank >= 1:
        bonus += 1

    if cv_years > required_years:
        bonus += 1

    score += bonus
    score = int(np.clip(score, 0, 100))
    
    return score, found_list, skills_score, dipl_score, exp_score

def get_connection(conn_str):
    # Normalize .NET connection string for pyodbc
    conn_str = conn_str.replace("=True", "=yes").replace("=False", "=no")
    if "Driver=" not in conn_str:
        conn_str = "Driver={ODBC Driver 17 for SQL Server};" + conn_str
    return pyodbc.connect(conn_str)

if __name__ == "__main__":
    if len(sys.argv) < 4:
        print(json.dumps({"error": "Usage: python score_calculation.py <cv_id> <offre_id> <connection_string>"}))
        sys.exit(1)

    cv_id = sys.argv[1]
    offre_id = sys.argv[2]
    conn_str = sys.argv[3]

    try:
        conn = get_connection(conn_str)
        cursor = conn.cursor()

        # Fetch CV details from structured storage first
        cursor.execute("SELECT Competences, Experiences, Diplomes FROM CvStructuredData WHERE CvId = ?", (cv_id,))
        cv_row = cursor.fetchone()

        if cv_row:
            competences_str = cv_row[0] or ""
            experience_str = cv_row[1] or ""
            diplomes_str = cv_row[2] or ""
        else:
            cursor.execute("SELECT Competences, Experience, NiveauEducation FROM Cv WHERE Id = ?", (cv_id,))
            cv_row = cursor.fetchone()
            if not cv_row:
                print(json.dumps({"error": "CV introuvable"}))
                sys.exit(0)

            competences_str = cv_row[0] or ""
            experience_str = cv_row[1] or ""
            diplomes_str = cv_row[2] or ""

        # Fetch Offre details
        cursor.execute("SELECT Description FROM OffresEmploi WHERE Id = ?", (offre_id,))
        offre_row = cursor.fetchone()
        if not offre_row:
            print(json.dumps({"error": "Offre introuvable"}))
            sys.exit(0)

        job_desc = offre_row[0] or ""

        # Format sections
        sections = {
            "Competences": [s.strip() for s in competences_str.split(",") if s.strip()],
            "Experiences": [s.strip() for s in experience_str.split("|") if s.strip()],
            "Diplomes": [s.strip() for s in diplomes_str.split("|") if s.strip()]
        }

        # text for keywords search
        full_cv_text = f"{competences_str} {experience_str} {diplomes_str}"

        # calculate score
        score, matched_keywords, skills_score, dipl_score, exp_score = calculate_match_score(job_desc, full_cv_text, sections)

        # Update Match table
        # Check if match exists
        cursor.execute("SELECT Id FROM Matches WHERE CvId = ? AND OffreId = ?", (cv_id, offre_id))
        match_row = cursor.fetchone()

        if match_row:
            cursor.execute("""
                UPDATE Matches 
                SET GlobalScore = ?, CompetenceScore = ?, ExperienceScore = ?, DiplomeScore = ?
                WHERE Id = ?
            """, (score, skills_score, exp_score, dipl_score, match_row[0]))
        else:
            cursor.execute("""
                INSERT INTO Matches (CvId, OffreId, GlobalScore, CompetenceScore, ExperienceScore, DiplomeScore)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (cv_id, offre_id, score, skills_score, exp_score, dipl_score))

        conn.commit()
        conn.close()

        output_data = {
            "success": True,
            "score": score,
            "details": {
                "skills": skills_score,
                "diplomas": dipl_score,
                "experience": exp_score
            },
            "matched_keywords": matched_keywords
        }

        print(json.dumps(output_data, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"error": f"Database or calculation error: {str(e)}"}), file=sys.stderr)
        sys.exit(1)
