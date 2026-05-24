import csv
import math
import sys
from collections import defaultdict
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import pandas as pd


def mean(values):
    return sum(values) / len(values) if values else 0.0


def stdev(values):
    if len(values) < 2:
        return 0.0
    average = mean(values)
    variance = sum((value - average) ** 2 for value in values) / (len(values) - 1)
    return math.sqrt(variance)


def main():
    if len(sys.argv) < 2:
        print("Usage: python Tools/analyze_custom_collision_benchmark.py path/to/custom_collision_benchmark.csv")
        return 1

    input_path = Path(sys.argv[1])
    summary_path = input_path.with_name(input_path.stem + "_summary.csv")
    write_summary(input_path, summary_path)
    write_charts(input_path, summary_path)
    return 0


def write_summary(input_path, summary_path):
    groups = defaultdict(lambda: {
        "stepMs": [],
        "pairChecks": [],
        "collisions": [],
        "triggerOverlaps": [],
        "spatialCellsUsed": [],
    })

    with input_path.open("r", newline="", encoding="utf-8-sig") as file:
        reader = csv.DictReader(file)
        for row in reader:
            key = (
                row["case"],
                row["scenario"],
                int(row["objectCount"]),
            )
            groups[key]["stepMs"].append(float(row["stepMs"]))
            groups[key]["pairChecks"].append(float(row["pairChecks"]))
            groups[key]["collisions"].append(float(row["collisions"]))
            groups[key]["triggerOverlaps"].append(float(row["triggerOverlaps"]))
            groups[key]["spatialCellsUsed"].append(float(row["spatialCellsUsed"]))

    with summary_path.open("w", newline="", encoding="utf-8") as file:
        writer = csv.writer(file)
        writer.writerow([
            "case",
            "scenario",
            "objectCount",
            "samples",
            "meanStepMs",
            "stdevStepMs",
            "stderrStepMs",
            "meanPairChecks",
            "meanCollisions",
            "meanTriggerOverlaps",
            "meanSpatialCellsUsed",
        ])

        for key, values in sorted(groups.items()):
            step_ms = values["stepMs"]
            deviation = stdev(step_ms)
            writer.writerow([
                *key,
                len(step_ms),
                f"{mean(step_ms):.6f}",
                f"{deviation:.6f}",
                f"{deviation / math.sqrt(len(step_ms)):.6f}" if step_ms else "0.000000",
                f"{mean(values['pairChecks']):.2f}",
                f"{mean(values['collisions']):.2f}",
                f"{mean(values['triggerOverlaps']):.2f}",
                f"{mean(values['spatialCellsUsed']):.2f}",
            ])

    print(f"Wrote summary: {summary_path}")


def write_charts(input_path, summary_path):
    summary = pd.read_csv(summary_path)
    chart_dir = input_path.with_name(input_path.stem + "_charts")
    chart_dir.mkdir(parents=True, exist_ok=True)

    plt.rcParams.update({
        "figure.figsize": (9, 5),
        "axes.grid": True,
        "grid.alpha": 0.25,
        "axes.spines.top": False,
        "axes.spines.right": False,
    })

    plot_lines(
        summary,
        chart_dir / "01_broadphase_time.png",
        "Broad phase performance: brute force vs spatial hash",
        "meanStepMs",
        "Mean custom collision step time (ms)",
        ["Brute force AABB", "Spatial hash AABB"],
    )
    plot_lines(
        summary,
        chart_dir / "02_pair_checks.png",
        "Pair checks: brute force vs spatial hash",
        "meanPairChecks",
        "Mean pair checks per step",
        ["Brute force AABB", "Spatial hash AABB"],
    )
    plot_lines(
        summary,
        chart_dir / "03_shape_comparison.png",
        "Shape comparison: AABB vs sphere",
        "meanStepMs",
        "Mean custom collision step time (ms)",
        ["Spatial hash AABB", "Spatial hash sphere"],
    )
    plot_lines(
        summary,
        chart_dir / "04_filtering_and_triggers.png",
        "Filtering and triggers",
        "meanStepMs",
        "Mean custom collision step time (ms)",
        ["Spatial hash AABB", "Layer filtered spatial hash", "Trigger only spatial hash"],
    )
    write_notes(summary, chart_dir)
    print(f"Wrote charts: {chart_dir}")


def plot_lines(summary, output_path, title, metric, ylabel, prefixes):
    fig, ax = plt.subplots()

    for prefix in prefixes:
        rows = rows_for_prefix(summary, prefix)
        if len(rows) == 0:
            continue

        y_error = rows["stderrStepMs"] if metric == "meanStepMs" else None
        ax.errorbar(
            rows["objectCount"],
            rows[metric],
            yerr=y_error,
            marker="o",
            linewidth=2,
            capsize=4 if y_error is not None else 0,
            label=prefix,
        )

    ax.set_title(title)
    ax.set_xlabel("Object count")
    ax.set_ylabel(ylabel)
    ax.legend()
    fig.tight_layout()
    fig.savefig(output_path, dpi=180)
    plt.close(fig)


def rows_for_prefix(summary, prefix):
    return summary[summary["case"].str.startswith(prefix)].sort_values("objectCount")


def value(summary, case_name, metric):
    rows = summary[summary["case"] == case_name]
    return float(rows.iloc[0][metric]) if len(rows) else 0.0


def ratio(value_a, value_b):
    return value_a / value_b if value_b else 0.0


def line_range(summary, prefix, metric):
    rows = rows_for_prefix(summary, prefix)
    if len(rows) == 0:
        return f"- {prefix}: no data."
    first = rows.iloc[0]
    last = rows.iloc[-1]
    return (
        f"- {prefix}: {first[metric]:.3f} at {int(first['objectCount'])} objects, "
        f"{last[metric]:.3f} at {int(last['objectCount'])} objects."
    )


def write_notes(summary, chart_dir):
    brute_1000 = value(summary, "Brute force AABB 1000", "meanStepMs")
    spatial_1000 = value(summary, "Spatial hash AABB 1000", "meanStepMs")
    brute_pairs_1000 = value(summary, "Brute force AABB 1000", "meanPairChecks")
    spatial_pairs_1000 = value(summary, "Spatial hash AABB 1000", "meanPairChecks")

    spatial_5000 = value(summary, "Spatial hash AABB 5000", "meanStepMs")
    sphere_5000 = value(summary, "Spatial hash sphere 5000", "meanStepMs")
    layer_5000 = value(summary, "Layer filtered spatial hash 5000", "meanStepMs")
    trigger_5000 = value(summary, "Trigger only spatial hash 5000", "meanStepMs")

    notes = chart_dir / "custom_collision_analysis.md"
    notes.write_text(
        "\n".join([
            "# Custom Collision Benchmark Analysis",
            "",
            "## What Was Tested",
            "",
            "This benchmark tests a custom 3D collision detection system implemented in Unity. The system does not use Unity colliders, rigidbodies, or `Physics.Simulate` for collision detection. Unity is only used to run the scripts and export the CSV data.",
            "",
            "The benchmark compares two broad-phase approaches: brute force and spatial hashing. Brute force checks every possible object pair. Spatial hashing divides the world into grid cells and only checks objects that share cells. It also compares AABB and sphere collision shapes, layer filtering, and trigger-only overlap detection.",
            "",
            "The main metric is `meanStepMs`, which is the average time needed to update movement, find possible collision pairs, test overlaps, and apply the simple collision response for one simulation step.",
            "",
            "## Chart 1: Broad Phase Performance",
            "",
            line_range(summary, "Brute force AABB", "meanStepMs"),
            line_range(summary, "Spatial hash AABB", "meanStepMs"),
            "",
            f"At 1000 objects, brute force takes {brute_1000:.3f} ms while spatial hash takes {spatial_1000:.3f} ms. Spatial hash is {ratio(brute_1000, spatial_1000):.2f}x faster at this object count.",
            "",
            "This chart is the most important result. Brute force becomes expensive quickly because the number of possible pairs grows with `O(n^2)`. Spatial hashing is faster because it reduces the number of pairs before the actual collision checks.",
            "",
            "## Chart 2: Pair Checks",
            "",
            line_range(summary, "Brute force AABB", "meanPairChecks"),
            line_range(summary, "Spatial hash AABB", "meanPairChecks"),
            "",
            f"At 1000 objects, brute force checks about {brute_pairs_1000:.0f} pairs per step. Spatial hash checks about {spatial_pairs_1000:.0f} pairs per step.",
            "",
            "This explains why spatial hash is faster: it avoids testing most pairs that are far away from each other. This chart connects the measured performance to the theoretical Big-O analysis.",
            "",
            "## Chart 3: Shape Comparison",
            "",
            line_range(summary, "Spatial hash AABB", "meanStepMs"),
            line_range(summary, "Spatial hash sphere", "meanStepMs"),
            "",
            f"At 5000 objects, spatial hash AABB takes {spatial_5000:.3f} ms and spatial hash sphere takes {sphere_5000:.3f} ms.",
            "",
            "This chart compares the narrow-phase collision tests. AABB checks use min/max axis overlap tests, while sphere checks use distance between centers. Both are simple, but their cost and number of detected overlaps can differ depending on object distribution.",
            "",
            "## Chart 4: Filtering and Triggers",
            "",
            line_range(summary, "Spatial hash AABB", "meanStepMs"),
            line_range(summary, "Layer filtered spatial hash", "meanStepMs"),
            line_range(summary, "Trigger only spatial hash", "meanStepMs"),
            "",
            f"At 5000 objects, normal spatial hash AABB takes {spatial_5000:.3f} ms, layer filtering takes {layer_5000:.3f} ms, and trigger-only detection takes {trigger_5000:.3f} ms.",
            "",
            "Layer filtering can reduce work because some pairs are rejected before narrow-phase collision checks. Trigger-only detection can also be cheaper because it records overlaps without applying collision response.",
            "",
            "## Overall Conclusion",
            "",
            "The results show that implementing a broad-phase optimisation is much more scalable than checking every possible pair. The custom spatial hash is the most important optimisation in this project. The tests also show that collision shape, filtering, and trigger behaviour affect performance, but the broad-phase choice has the largest impact.",
        ]),
        encoding="utf-8",
    )


if __name__ == "__main__":
    raise SystemExit(main())
