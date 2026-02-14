"""
Feature Engineering Script for Adaptive Difficulty System
Author: Izzat's FYP Project
Purpose: Generate engineered features from gameplay data for ML training

This script processes raw gameplay data and creates derived features
that better represent player performance for difficulty prediction.
"""

import pandas as pd
import numpy as np
from pathlib import Path
import warnings
warnings.filterwarnings('ignore')


def load_data(filepath):
    """Load raw gameplay data"""
    df = pd.read_csv(filepath)
    print(f" Loaded {len(df)} sessions from {filepath}")
    return df


def split_by_game_type(df):
    """Split data into fighting and racing game datasets"""
    racing_metrics = [
        'lapsCompleted', 'bestLapTime', 'avgLapTime', 'totalRaceTime', 
        'collisions', 'maxSpeed', 'avgSpeed', 'consistency'
    ]
    
    fighting_metrics = [
        'victory', 'combosExecuted', 'perfectDodges', 'hitsDealt', 
        'hitsTaken', 'playerAccuracy', 'avgReactionTime'
    ]
    
    fighting_df = df[df['gameType'] == 'Fighting'].drop(columns=racing_metrics)
    racing_df = df[df['gameType'] == 'Racing'].drop(columns=fighting_metrics)
    
    print(f"\n Split data:")
    print(f"  - Fighting sessions: {len(fighting_df)}")
    print(f"  - Racing sessions: {len(racing_df)}")
    
    return fighting_df, racing_df


def engineer_fighting_features(fighting_df):
    """
    Create derived features for fighting game
    
    Features capture:
    - Offensive performance (damage output)
    - Defensive performance (damage taken, dodges)
    - Skill execution (combos, reaction time)
    - Overall combat effectiveness
    """
    print("\n" + "="*70)
    print("FIGHTING GAME: FEATURE ENGINEERING")
    print("="*70)
    
    features = fighting_df.copy()
    
    # 1. Combat Efficiency: Offensive vs Defensive Performance
    # Higher values = better offense/defense ratio
    features['combatEfficiency'] = (
        features['hitsDealt'] / (features['hitsTaken'] + 1)
    )
    print(" combatEfficiency = hitsDealt / (hitsTaken + 1)")
    
    # 2. Damage Output Rate
    # Measures offensive pressure (higher = more aggressive)
    features['damagePerSecond'] = (
        features['hitsDealt'] / features['sessionDuration']
    )
    print(" damagePerSecond = hitsDealt / sessionDuration")
    
    # 3. Damage Intake Rate
    # Measures defensive weakness (lower = better defense)
    features['damageIntakeRate'] = (
        features['hitsTaken'] / features['sessionDuration']
    )
    print(" damageIntakeRate = hitsTaken / sessionDuration")
    
    # 4. Survival Rate
    # Binary indicator (1 = survived, 0 = died)
    features['survivalRate'] = np.where(features['deaths'] == 0, 1, 0)
    print(" survivalRate = 1 if alive, 0 if died")
    
    # 5. Combo Execution Rate
    # Combos per minute - measures offensive skill
    features['comboRate'] = (
        features['combosExecuted'] / features['sessionDuration']
    ) * 60
    print(" comboRate = (combosExecuted / duration) * 60")
    
    # 6. Perfect Dodge Rate
    # Dodges per minute - measures defensive skill
    features['dodgeRate'] = (
        features['perfectDodges'] / features['sessionDuration']
    ) * 60
    print(" dodgeRate = (perfectDodges / duration) * 60")
    
    # 7. Reaction Performance Score
    # Inverse of reaction time (lower time = higher score)
    features['reactionScore'] = 1 / (features['avgReactionTime'] + 0.001)
    print(" reactionScore = 1 / (avgReactionTime + 0.001)")
    
    # 8. Overall Combat Performance Index
    # Composite metric combining multiple aspects (0-1 scale)
    features['combatPerformanceIndex'] = (
        features['playerAccuracy'] * 0.3 +
        (features['combatEfficiency'] / features['combatEfficiency'].max()) * 0.3 +
        (features['comboRate'] / features['comboRate'].max()) * 0.2 +
        (features['dodgeRate'] / features['dodgeRate'].max()) * 0.2
    )
    print(" combatPerformanceIndex = weighted composite (accuracy + efficiency + combos + dodges)")
    
    # 9. Efficiency Per Accuracy
    # How efficient player is relative to their accuracy
    features['efficiencyPerAccuracy'] = (
        features['combatEfficiency'] / (features['playerAccuracy'] + 0.1)
    )
    print(" efficiencyPerAccuracy = combatEfficiency / (playerAccuracy + 0.1)")
    
    print(f"\n Total features: {len(features.columns)}")
    print(f" Engineered features: 9")
    
    return features


def engineer_racing_features(racing_df):
    """
    Create derived features for racing game
    
    Features capture:
    - Speed performance
    - Consistency/smoothness
    - Collision avoidance
    - Time efficiency
    """
    print("\n" + "="*70)
    print("RACING GAME: FEATURE ENGINEERING")
    print("="*70)
    
    features = racing_df.copy()
    
    # 1. Speed Efficiency
    # How close to max speed the player maintains
    features['speedEfficiency'] = features['avgSpeed'] / features['maxSpeed']
    print(" speedEfficiency = avgSpeed / maxSpeed")
    
    # 2. Collision Rate
    # Collisions per lap (lower = better)
    features['collisionRate'] = (
        features['collisions'] / (features['lapsCompleted'] + 1)
    )
    print(" collisionRate = collisions / (lapsCompleted + 1)")
    
    # 3. Lap Time Variance
    # Difference between best and average lap time
    features['lapTimeVariance'] = (
        features['avgLapTime'] - features['bestLapTime']
    )
    print(" lapTimeVariance = avgLapTime - bestLapTime")
    
    # 4. Completion Rate
    # Binary indicator (1 = finished, 0 = DNF)
    features['completionRate'] = features['completed'].astype(int)
    print(" completionRate = 1 if completed, 0 if DNF")
    
    # 5. Driving Smoothness
    # Inverse of collision rate (higher = smoother)
    features['drivingSmoothness'] = 1 / (features['collisionRate'] + 0.1)
    print(" drivingSmoothness = 1 / (collisionRate + 0.1)")
    
    # 6. Time Efficiency
    # Best lap time relative to average (lower = more consistent)
    features['timeEfficiency'] = (
        features['bestLapTime'] / (features['avgLapTime'] + 0.1)
    )
    print(" timeEfficiency = bestLapTime / (avgLapTime + 0.1)")
    
    # 7. Speed-Consistency Score
    # Combines speed efficiency and consistency
    features['speedConsistencyScore'] = (
        features['speedEfficiency'] * features['consistency']
    )
    print(" speedConsistencyScore = speedEfficiency * consistency")
    
    # 8. Racing Performance Index
    # Composite metric (0-1 scale)
    features['racingPerformanceIndex'] = (
        features['speedEfficiency'] * 0.3 +
        features['consistency'] * 0.3 +
        (1 - features['collisionRate'] / features['collisionRate'].max()) * 0.2 +
        features['completionRate'] * 0.2
    )
    print(" racingPerformanceIndex = weighted composite (speed + consistency + collisions + completion)")
    
    # 9. Clean Racing Score
    # Rewards low collisions and high completion
    features['cleanRacingScore'] = (
        features['completionRate'] * (1 - features['collisionRate'] / features['collisionRate'].max())
    )
    print(" cleanRacingScore = completionRate * (1 - normalized collision rate)")
    
    print(f"\n Total features: {len(features.columns)}")
    print(f" Engineered features: 9")
    
    return features


def save_features(fighting_features, racing_features, output_dir=r"Assets/Data/Processed"):
    """Save engineered features to CSV files"""
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Save fighting features
    fighting_output = output_path / 'fighting_features_engineered.csv'
    fighting_features.to_csv(fighting_output, index=False)
    print(f"\n Saved fighting features: {fighting_output}")
    print(f"  Shape: {fighting_features.shape}")
    
    # Save racing features
    racing_output = output_path / 'racing_features_engineered.csv'
    racing_features.to_csv(racing_output, index=False)
    print(f" Saved racing features: {racing_output}")
    print(f"  Shape: {racing_features.shape}")
    
    return fighting_output, racing_output


def print_summary(fighting_features, racing_features):
    """Print feature engineering summary"""
    print("\n" + "="*70)
    print("FEATURE ENGINEERING SUMMARY")
    print("="*70)
    
    print(f"\n{'FIGHTING GAME':-^70}")
    print(f"Total sessions: {len(fighting_features)}")
    print(f"Total features: {len(fighting_features.columns)}")
    print(f"\nEngineered features (9):")
    engineered = [
        'combatEfficiency', 'damagePerSecond', 'damageIntakeRate',
        'survivalRate', 'comboRate', 'dodgeRate', 'reactionScore',
        'combatPerformanceIndex', 'efficiencyPerAccuracy'
    ]
    for i, feat in enumerate(engineered, 1):
        print(f"  {i}. {feat}")
    
    print(f"\n{'RACING GAME':-^70}")
    print(f"Total sessions: {len(racing_features)}")
    print(f"Total features: {len(racing_features.columns)}")
    print(f"\nEngineered features (9):")
    racing_engineered = [
        'speedEfficiency', 'collisionRate', 'lapTimeVariance',
        'completionRate', 'drivingSmoothness', 'timeEfficiency',
        'speedConsistencyScore', 'racingPerformanceIndex', 'cleanRacingScore'
    ]
    for i, feat in enumerate(racing_engineered, 1):
        print(f"  {i}. {feat}")
    
    print("\n" + "="*70)
    print("READY FOR ML TRAINING")
    print("="*70)
    print("\nNext steps:")
    print("1. Features are now ready for Random Forest training")
    print("2. Target variable: 'feedback' (TooEasy/Balanced/TooHard)")
    print("3. Consider feature selection if needed")
    print("4. Train separate models for Fighting and Racing games")
    print("="*70 + "\n")


def main():
    """Main execution function"""
    print("\n" + "="*70)
    print("ADAPTIVE DIFFICULTY SYSTEM - FEATURE ENGINEERING")
    print("="*70 + "\n")
    
    # Load data
    df = load_data(r'Assets/Data/Raw/GameplayData.csv')
    
    # Split by game type
    fighting_df, racing_df = split_by_game_type(df)
    
    # Engineer features
    fighting_features = engineer_fighting_features(fighting_df)
    racing_features = engineer_racing_features(racing_df)
    
    # Save to CSV
    save_features(fighting_features, racing_features)
    
    # Print summary
    print_summary(fighting_features, racing_features)
    
    print("Feature engineering complete!")


if __name__ == "__main__":
    main()