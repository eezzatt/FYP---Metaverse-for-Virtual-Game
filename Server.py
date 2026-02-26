"""
Adaptive Difficulty System - Flask Prediction Server
Author: Izzat FYP Project
Purpose: Serve ML predictions and XAI explanations to Unity via HTTP

Endpoints:
    POST /predict  - Accepts session data, returns difficulty recommendation + XAI
    GET  /health   - Health check
"""

from flask import Flask, request, jsonify
import pandas as pd
import numpy as np
import os
import joblib

app = Flask(__name__)

# ─── Load Models & Scaler ────────────────────────────────────────────────────

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.join(BASE_DIR, "Assets", "Models")

with open(os.path.join(MODELS_DIR, "fighting_model.pkl"), "rb") as f:
    fighting_model = joblib.load(f)

with open(os.path.join(MODELS_DIR, "racing_model.pkl"), "rb") as f:
    racing_model = joblib.load(f)

# ─── Feature Definitions ─────────────────────────────────────────────────────

FIGHTING_FEATURES = [
    "combatEfficiency",
    "damagePerSecond",
    "damageIntakeRate",
    "comboRate",
    "dodgeRate",
    "reactionScore",
    "combatPerformanceIndex",
    "sessionDuration",
    "playerAccuracy",
    "victory",
]

RACING_FEATURES = [
    "speedEfficiency",
    "collisionRate",
    "drivingSmoothness",
    "timeEfficiency",
    "racingPerformanceIndex",
    "cleanRacingScore",
    "consistency",
    "lapsCompleted",
]

# Human-readable display names for XAI panel
FEATURE_DISPLAY_NAMES = {
    "combatEfficiency":         "Combat Efficiency",
    "damagePerSecond":          "Damage Output Rate",
    "damageIntakeRate":         "Damage Taken Rate",
    "comboRate":                "Combo Rate",
    "dodgeRate":                "Dodge Rate",
    "reactionScore":            "Reaction Speed",
    "combatPerformanceIndex":   "Overall Combat Score",
    "sessionDuration":          "Session Duration",
    "playerAccuracy":           "Attack Accuracy",
    "victory":                  "Match Won",
    "speedEfficiency":          "Speed Efficiency",
    "collisionRate":            "Collision Rate",
    "drivingSmoothness":        "Driving Smoothness",
    "timeEfficiency":           "Lap Time Efficiency",
    "racingPerformanceIndex":   "Overall Racing Score",
    "cleanRacingScore":         "Clean Racing Score",
    "consistency":              "Lap Consistency",
    "lapsCompleted":            "Laps Completed",
}

# ─── Feature Engineering ─────────────────────────────────────────────────────

def engineer_fighting_features(raw: dict) -> dict:
    """Mirror the Python feature engineering for fighting game."""
    duration      = raw.get("sessionDuration", 1.0)
    hits_dealt    = raw.get("hitsDealt", 0.0)
    hits_taken    = raw.get("hitsTaken", 0.0)
    combos        = raw.get("combosExecuted", 0.0)
    dodges        = raw.get("perfectDodges", 0.0)
    reaction_time = raw.get("avgReactionTime", 0.0)
    accuracy      = raw.get("playerAccuracy", 0.0)
    victory       = raw.get("victory", 0.0)

    combat_efficiency    = hits_dealt / (hits_taken + 1)
    damage_per_second    = hits_dealt / duration
    damage_intake_rate   = hits_taken / duration
    combo_rate           = (combos / duration) * 60
    dodge_rate           = (dodges / duration) * 60
    reaction_score       = 1.0 / (reaction_time + 0.001)

    # combatPerformanceIndex requires normalisation denominators — we use
    # sensible per-session approximations (same formulas, no dataset max needed)
    combat_perf_index = (
        accuracy * 0.3
        + (combat_efficiency / (combat_efficiency + 1)) * 0.3   # soft-normalise
        + min(combo_rate / 20.0, 1.0) * 0.2                    # 20 combos/min = max
        + min(dodge_rate / 10.0, 1.0) * 0.2                    # 10 dodges/min = max
    )

    return {
        "combatEfficiency":       combat_efficiency,
        "damagePerSecond":        damage_per_second,
        "damageIntakeRate":       damage_intake_rate,
        "comboRate":              combo_rate,
        "dodgeRate":              dodge_rate,
        "reactionScore":          reaction_score,
        "combatPerformanceIndex": combat_perf_index,
        "sessionDuration":        duration,
        "playerAccuracy":         accuracy,
        "victory":                float(victory),
    }


def engineer_racing_features(raw: dict) -> dict:
    """Mirror the Python feature engineering for racing game."""
    avg_speed      = raw.get("avgSpeed", 0.0)
    max_speed      = raw.get("maxSpeed", 0.001)
    collisions     = raw.get("collisions", 0.0)
    laps_completed = raw.get("lapsCompleted", 0.0)
    best_lap       = raw.get("bestLapTime", 0.0)
    avg_lap        = raw.get("avgLapTime", 0.1)
    consistency    = raw.get("consistency", 0.0)
    completed      = raw.get("completed", 0.0)

    speed_efficiency   = avg_speed / (max_speed + 0.001)
    collision_rate     = collisions / (laps_completed + 1)
    driving_smoothness = 1.0 / (collision_rate + 0.1)
    time_efficiency    = best_lap / (avg_lap + 0.1)
    completion_rate    = float(completed)

    racing_perf_index = (
        speed_efficiency * 0.3
        + consistency * 0.3
        + (1.0 - min(collision_rate / 5.0, 1.0)) * 0.2   # 5 collisions/lap = worst
        + completion_rate * 0.2
    )

    clean_racing_score = completion_rate * (1.0 - min(collision_rate / 5.0, 1.0))

    return {
        "speedEfficiency":        speed_efficiency,
        "collisionRate":          collision_rate,
        "drivingSmoothness":      driving_smoothness,
        "timeEfficiency":         time_efficiency,
        "racingPerformanceIndex": racing_perf_index,
        "cleanRacingScore":       clean_racing_score,
        "consistency":            consistency,
        "lapsCompleted":          float(laps_completed),
    }


# ─── XAI Helper ──────────────────────────────────────────────────────────────

def get_top_features(model, feature_names: list, feature_values: list, top_n: int = 3) -> list:
    """
    Return the top N most influential features for this prediction.
    Returns a list of dicts: {name, displayName, importance, value}
    """
    rf_model = model.named_steps['rf']
    importances = rf_model.feature_importances_
    ranked = sorted(
        zip(feature_names, importances, feature_values),
        key=lambda x: x[1],
        reverse=True
    )[:top_n]

    return [
        {
            "name":        feat,
            "displayName": FEATURE_DISPLAY_NAMES.get(feat, feat),
            "importance":  round(float(imp), 4),
            "value":       round(float(val), 4),
        }
        for feat, imp, val in ranked
    ]


def build_xai_description(prediction: str, top_features: list) -> str:
    """Generate a plain-language explanation sentence."""
    feat_names = ", ".join(f["displayName"] for f in top_features)
    messages = {
        "TooEasy": f"The game was too easy for you. Your performance — especially {feat_names} — shows you can handle a harder challenge.",
        "Balanced": f"The difficulty feels right. Key indicators like {feat_names} suggest you are in the optimal challenge zone.",
        "TooHard": f"The difficulty was too high. Factors like {feat_names} indicate the game may benefit from an easier setting.",
    }
    return messages.get(prediction, f"Recommendation based on {feat_names}.")


# ─── Routes ──────────────────────────────────────────────────────────────────

@app.route("/health", methods=["GET"])
def health():
    return jsonify({"status": "ok", "models": ["fighting", "racing"]}), 200


@app.route("/predict", methods=["POST"])
def predict():
    data = request.get_json(force=True)
    
    # DEBUG: Print what Unity sent
    print("Received data:", data)
    print("Data keys:", data.keys() if isinstance(data, dict) else "Not a dict!")

    game_type = data.get("gameType", "").strip()
    if game_type not in ("Fighting", "Racing"):
        return jsonify({"error": f"Unknown gameType: '{game_type}'"}), 400

    try:
        if game_type == "Fighting":
            features_dict  = engineer_fighting_features(data)
            feature_names  = FIGHTING_FEATURES
            model          = fighting_model 
        else:
            features_dict  = engineer_racing_features(data)
            feature_names  = RACING_FEATURES
            model          = racing_model

        # Build ordered feature vector
        features_df = pd.DataFrame([features_dict], columns=feature_names)

        # Predict
        prediction   = model.predict(features_df)[0]          # e.g. "TooHard"
        probabilities = model.predict_proba(features_df)[0]
        confidence   = round(float(max(probabilities)), 4)
        class_labels = list(model.named_steps['rf'].classes_)
        prob_map     = {cls: round(float(p), 4) for cls, p in zip(class_labels, probabilities)}

        # XAI
        raw_values   = [features_dict[f] for f in feature_names]
        top_features = get_top_features(model, feature_names, raw_values, top_n=3)
        explanation  = build_xai_description(prediction, top_features)

        return jsonify({
            "prediction":    prediction,
            "confidence":    confidence,
            "probabilities": prob_map,
            "topFeatures":   top_features,
            "explanation":   explanation,
        }), 200

    except KeyError as e:
        return jsonify({"error": f"Missing field in request: {e}"}), 422
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# ─── Main ─────────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    print("=" * 60)
    print("  Adaptive Difficulty Server")
    print("  Running on http://localhost:5000")
    print("=" * 60)
    app.run(host="0.0.0.0", port=5000, debug=False)