"""
PIDSS Phase 2 — Schema Validation Script
=========================================
Validates all example payloads and fixtures against their corresponding JSON Schema Draft-07 definitions.

Usage:
    python validate_schemas.py

Exit codes:
    0 — all validations passed
    1 — one or more validations failed

Requirements:
    pip install jsonschema
"""

import json
import sys
from pathlib import Path

try:
    import jsonschema
    from jsonschema import Draft7Validator, validate, ValidationError
except ImportError:
    print("ERROR: jsonschema library not installed. Run: pip install jsonschema")
    sys.exit(1)


# ─── Path resolution ──────────────────────────────────────────────────────────

SCRIPT_DIR = Path(__file__).parent
DATA_DIR = SCRIPT_DIR.parent
SCHEMAS_DIR = DATA_DIR / "schemas"
CONTRACTS_DIR = DATA_DIR / "contracts"
FIXTURES_DIR = SCRIPT_DIR / "fixtures"


# ─── Helpers ──────────────────────────────────────────────────────────────────

def load_json(path: Path) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    # Strip _comment fields before validation (they are documentation-only)
    if isinstance(data, dict):
        data.pop("_comment", None)
    return data


def run_check(label: str, instance_path: Path, schema_path: Path, expect_valid: bool) -> bool:
    """
    Validates instance against schema.
    Returns True if the check result matches expectation.
    """
    try:
        instance = load_json(instance_path)
        schema = load_json(schema_path)
    except FileNotFoundError as e:
        print(f"  [ERROR] File not found: {e}")
        return False
    except json.JSONDecodeError as e:
        print(f"  [ERROR] JSON parse error in {instance_path}: {e}")
        return False

    validator = Draft7Validator(schema)
    errors = list(validator.iter_errors(instance))

    if expect_valid:
        if not errors:
            print(f"  [PASS]  {label}")
            return True
        else:
            print(f"  [FAIL]  {label} — expected VALID but got {len(errors)} error(s):")
            for err in errors[:3]:
                print(f"          • {err.json_path}: {err.message}")
            return False
    else:
        if errors:
            print(f"  [PASS]  {label} — correctly rejected ({len(errors)} validation error(s))")
            return True
        else:
            print(f"  [FAIL]  {label} — expected INVALID but schema accepted it")
            return False


# ─── Test plan ────────────────────────────────────────────────────────────────

def build_checks() -> list[tuple]:
    """
    Returns a list of (label, instance_path, schema_path, expect_valid) tuples.
    """
    scenario_schema = SCHEMAS_DIR / "scenario.v1.schema.json"
    sim_result_schema = SCHEMAS_DIR / "simulation_result.v1.schema.json"
    analysis_schema = SCHEMAS_DIR / "analysis_response.v1.schema.json"
    recommendation_schema = SCHEMAS_DIR / "recommendation.v1.schema.json"

    return [
        # ── Contract examples (must be valid) ──────────────────────────────
        (
            "scenario.v1.example.json is valid",
            CONTRACTS_DIR / "scenario.v1.example.json",
            scenario_schema,
            True,
        ),
        (
            "simulation_result.v1.example.json is valid",
            CONTRACTS_DIR / "simulation_result.v1.example.json",
            sim_result_schema,
            True,
        ),
        (
            "analysis_response.v1.example.json is valid",
            CONTRACTS_DIR / "analysis_response.v1.example.json",
            analysis_schema,
            True,
        ),
        (
            "recommendation.v1.example.json is valid",
            CONTRACTS_DIR / "recommendation.v1.example.json",
            recommendation_schema,
            True,
        ),
        # ── canonical_scenario.example.json is NOT validated against public schema ──
        # It is the canonical (internal) format and does not contain schema_version.
        # Canonical validation is the Adapter's responsibility, not this script.

        # ── Positive fixtures (must be valid) ──────────────────────────────
        (
            "fixture: scenario.v1.valid.json is valid",
            FIXTURES_DIR / "scenario.v1.valid.json",
            scenario_schema,
            True,
        ),
        # ── Negative fixtures (must be rejected) ───────────────────────────
        (
            "fixture: scenario.v1.invalid_missing_required.json is rejected",
            FIXTURES_DIR / "scenario.v1.invalid_missing_required.json",
            scenario_schema,
            False,
        ),
        (
            "fixture: scenario.v1.invalid_additional_properties.json is rejected",
            FIXTURES_DIR / "scenario.v1.invalid_additional_properties.json",
            scenario_schema,
            False,
        ),
        (
            "fixture: scenario.v1.invalid_bad_enum.json is rejected",
            FIXTURES_DIR / "scenario.v1.invalid_bad_enum.json",
            scenario_schema,
            False,
        ),
        (
            "fixture: scenario.v1.invalid_empty_covered_stage_ids.json is rejected",
            FIXTURES_DIR / "scenario.v1.invalid_empty_covered_stage_ids.json",
            scenario_schema,
            False,
        ),
        (
            "fixture: scenario.v1.invalid_missing_bom.json is rejected",
            FIXTURES_DIR / "scenario.v1.invalid_missing_bom.json",
            scenario_schema,
            False,
        ),
        (
            "fixture: scenario.v1.invalid_missing_work_unit_parameters.json is rejected",
            FIXTURES_DIR / "scenario.v1.invalid_missing_work_unit_parameters.json",
            scenario_schema,
            False,
        ),
    ]


# ─── Main ─────────────────────────────────────────────────────────────────────

def main() -> int:
    print("=" * 60)
    print("PIDSS Phase 2 — Schema Validation")
    print("=" * 60)

    checks = build_checks()
    total = len(checks)
    passed = 0
    failed = 0

    print(f"\nRunning {total} checks...\n")

    for label, instance_path, schema_path, expect_valid in checks:
        ok = run_check(label, instance_path, schema_path, expect_valid)
        if ok:
            passed += 1
        else:
            failed += 1

    print("\n" + "=" * 60)
    print(f"Results: {passed}/{total} passed, {failed} failed")
    print("=" * 60)

    if failed > 0:
        print("\n[FAIL] One or more schema checks failed.")
        return 1
    else:
        print("\n[PASS] All schema checks passed.")
        return 0


if __name__ == "__main__":
    sys.exit(main())
