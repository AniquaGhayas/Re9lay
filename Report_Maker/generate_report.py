"""
NeuroPlay 2.0 - Progress Report Generator (CLI entry point)

Usage:
    python generate_report.py session.csv --out report.pdf --emg-threshold 400

Reads a Unity-logged session CSV (20Hz), computes EMG/motion/score
metrics, generates charts, and assembles a PDF progress report.
This script is standalone - run it after a session, separately from Unity.
"""

import argparse
import os
import tempfile

from metrics import load_session, compute_all_metrics
from charts import generate_all_charts
from report import build_report


def main():
    parser = argparse.ArgumentParser(description="Generate NeuroPlay 2.0 progress report PDF")
    parser.add_argument("csv_path", help="Path to session CSV")
    parser.add_argument("--out", default="progress_report.pdf", help="Output PDF path")
    parser.add_argument("--emg-threshold", type=float, default=400.0,
                         help="EMG value threshold for contraction detection")
    parser.add_argument("--session-label", default=None, help="Label shown on report")
    parser.add_argument("--logo", default=None, help="Path to logo image (png/jpg) for the header")
    args = parser.parse_args()

    df = load_session(args.csv_path)
    metrics = compute_all_metrics(df, emg_threshold=args.emg_threshold)

    with tempfile.TemporaryDirectory() as chart_dir:
        chart_paths = generate_all_charts(df, metrics, chart_dir, emg_threshold=args.emg_threshold)
        build_report(metrics, chart_paths, args.out, session_label=args.session_label,
                     logo_path=args.logo)

    print(f"Report written to {os.path.abspath(args.out)}")


if __name__ == "__main__":
    main()
