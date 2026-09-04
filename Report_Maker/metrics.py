"""
NeuroPlay 2.0 - Session metrics computation.
Reads a session CSV (20Hz, columns: timestamp, pitch, roll, emg_value,
player_x, player_y, score, game_speed, shoot_state) and computes the
EMG, motion, and score/difficulty metrics used in the progress report.
"""

import numpy as np
import pandas as pd


def load_session(csv_path: str) -> pd.DataFrame:
    """Load a session CSV and convert timestamp to elapsed seconds."""
    df = pd.read_csv(csv_path)
    df.columns = [c.strip() for c in df.columns]

    # timestamp format: HH_MM_SS_mmm -> elapsed seconds from first row
    def to_seconds(ts):
        h, m, s, ms = [int(x) for x in str(ts).split("_")]
        return h * 3600 + m * 60 + s + ms / 1000.0

    df["t_sec"] = df["timestamp"].apply(to_seconds)
    df["t_sec"] = df["t_sec"] - df["t_sec"].iloc[0]
    return df


def compute_emg_metrics(df: pd.DataFrame, emg_threshold: float = 400.0) -> dict:
    """EMG contraction metrics. Falls back to shoot_state if it already
    encodes contraction; emg_threshold is used to double-check/derive
    contraction windows directly from the raw signal."""
    contracted = (df["emg_value"] >= emg_threshold).astype(int)
    edges = contracted.diff().fillna(0)

    rising = df.index[edges == 1].tolist()
    falling = df.index[edges == -1].tolist()

    # pair up rising/falling edges into contraction windows
    durations = []
    for r in rising:
        f_candidates = [f for f in falling if f > r]
        f = f_candidates[0] if f_candidates else df.index[-1]
        durations.append(df["t_sec"].iloc[f] - df["t_sec"].iloc[r])

    session_duration = df["t_sec"].iloc[-1] - df["t_sec"].iloc[0]
    duty_cycle = contracted.mean() * 100 if len(contracted) else 0.0
    contraction_count = len(rising)
    freq_per_min = (
        contraction_count / (session_duration / 60) if session_duration > 0 else 0.0
    )

    return {
        "contraction_count": contraction_count,
        "mean_contraction_duration_s": float(np.mean(durations)) if durations else 0.0,
        "duty_cycle_pct": float(duty_cycle),
        "mean_emg": float(df["emg_value"].mean()),
        "peak_emg": float(df["emg_value"].max()),
        "contraction_freq_per_min": float(freq_per_min),
        "contracted_mask": contracted,
    }


def compute_motion_metrics(df: pd.DataFrame) -> dict:
    """Distance, speed, acceleration, jerk, path straightness, range of motion."""
    dx = df["player_x"].diff().fillna(0)
    dy = df["player_y"].diff().fillna(0)
    dt = df["t_sec"].diff().fillna(0).replace(0, np.nan)

    step_dist = np.sqrt(dx**2 + dy**2)
    speed = (step_dist / dt).fillna(0)
    accel = (speed.diff() / dt).fillna(0)
    jerk = (accel.diff() / dt).fillna(0)

    total_dist = step_dist.sum()
    straight_dist = np.sqrt(
        (df["player_x"].iloc[-1] - df["player_x"].iloc[0]) ** 2
        + (df["player_y"].iloc[-1] - df["player_y"].iloc[0]) ** 2
    )
    straightness = straight_dist / total_dist if total_dist > 0 else 1.0

    df["speed"] = speed
    df["accel"] = accel
    df["jerk"] = jerk

    return {
        "total_distance": float(total_dist),
        "mean_speed": float(speed.mean()),
        "peak_speed": float(speed.max()),
        "path_straightness": float(min(straightness, 1.0)),
        "pitch_range": (float(df["pitch"].min()), float(df["pitch"].max())),
        "roll_range": (float(df["roll"].min()), float(df["roll"].max())),
    }


def compute_score_metrics(df: pd.DataFrame) -> dict:
    """Score progression, speed-tier timing, score rate."""
    session_duration = df["t_sec"].iloc[-1] - df["t_sec"].iloc[0]
    final_score = df["score"].iloc[-1]
    score_rate = final_score / (session_duration / 60) if session_duration > 0 else 0.0

    tier_times = {}
    for speed_val in sorted(df["game_speed"].unique()):
        first_row = df[df["game_speed"] == speed_val].iloc[0]
        tier_times[float(speed_val)] = float(first_row["t_sec"])

    return {
        "final_score": int(final_score),
        "session_duration_s": float(session_duration),
        "score_rate_per_min": float(score_rate),
        "speed_tier_times": tier_times,
    }


def compute_accuracy_metrics(df: pd.DataFrame) -> dict:
    """Rough shot-accuracy proxy: score increments vs shoot_state=1 rows."""
    score_increments = df["score"].diff().fillna(0)
    hits = int((score_increments > 0).sum())
    shooting_rows = int((df["shoot_state"] == 1).sum())
    hit_rate = (hits / shooting_rows * 100) if shooting_rows > 0 else 0.0
    return {
        "hits": hits,
        "shooting_samples": shooting_rows,
        "hit_rate_pct": float(hit_rate),
    }


def compute_all_metrics(df: pd.DataFrame, emg_threshold: float = 400.0) -> dict:
    return {
        "emg": compute_emg_metrics(df, emg_threshold),
        "motion": compute_motion_metrics(df),
        "score": compute_score_metrics(df),
        "accuracy": compute_accuracy_metrics(df),
    }
