"""
Generate patent figures as UML diagrams from PlantUML source.
Produces 4 PNG figures for the patent application.

Requirements:
  - Java + plantuml.jar: java -jar plantuml.jar -tpng file.puml
  - OR: pip install plantuml  (uses PlantUML server)
  - OR: pip install pythonplantuml
"""

import os
import subprocess
import sys

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DIAGRAMS_DIR = os.path.join(SCRIPT_DIR, 'diagrams')
OUTPUT_DIR = SCRIPT_DIR

FIGURES = [
    ('fig1_component.puml', 'fig1_system_structure.png'),
    ('fig2_sequence.puml', 'fig2_online_cme_flow.png'),
    ('fig3_activity.puml', 'fig3_metaheuristic_loop.png'),
    ('fig4_structure.puml', 'fig4_spectral_features.png'),
]


def find_plantuml_jar():
    """Find plantuml.jar in common locations."""
    candidates = [
        os.path.join(SCRIPT_DIR, 'plantuml.jar'),
        os.path.join(SCRIPT_DIR, 'diagrams', 'plantuml.jar'),
        os.path.expanduser('~/plantuml.jar'),
        '/usr/share/plantuml/plantuml.jar',
    ]
    for path in candidates:
        if os.path.isfile(path):
            return path
    return None


def render_with_java(puml_path, png_path):
    """Render using Java + plantuml.jar. Output filename from @startuml name."""
    jar = find_plantuml_jar()
    if not jar:
        return False
    out_dir = os.path.dirname(png_path)
    try:
        result = subprocess.run(
            ['java', '-jar', jar, '-tpng', '-o', out_dir, puml_path],
            capture_output=True,
            text=True,
            timeout=60,
        )
        if result.returncode != 0:
            print(result.stderr, file=sys.stderr)
            return False
        # PlantUML uses @startuml name for output; our FIGURES map puml->png names
        expected_name = os.path.basename(png_path)
        if os.path.isfile(os.path.join(out_dir, expected_name)):
            return True
        # Fallback: output may use puml base name
        puml_base = os.path.splitext(os.path.basename(puml_path))[0]
        alt_path = os.path.join(out_dir, puml_base + '.png')
        if os.path.isfile(alt_path) and alt_path != png_path:
            os.replace(alt_path, png_path)
        return os.path.isfile(png_path)
    except (subprocess.TimeoutExpired, FileNotFoundError) as e:
        print(f"Java/PlantUML error: {e}", file=sys.stderr)
        return False


def render_with_plantuml_package(puml_path, png_path):
    """Render using plantuml Python package (uses web server)."""
    try:
        import plantuml
        with open(puml_path, 'r', encoding='utf-8') as f:
            diagram = f.read()
        server = plantuml.PlantUML(url='http://www.plantuml.com/plantuml/png/')
        result = server.processes(diagram)
        if result and len(result) > 0:
            with open(png_path, 'wb') as f:
                f.write(result)
            return True
    except ImportError:
        pass
    except Exception as e:
        print(f"PlantUML package error: {e}", file=sys.stderr)
    return False


def render_with_pythonplantuml(puml_path, png_path):
    """Render using pythonplantuml package."""
    try:
        from pythonplantuml import generate_uml_png
        with open(puml_path, 'r', encoding='utf-8') as f:
            uml_code = f.read()
        generate_uml_png(uml_code, png_path)
        return os.path.isfile(png_path)
    except ImportError:
        pass
    except Exception as e:
        print(f"pythonplantuml error: {e}", file=sys.stderr)
    return False


def render_diagram(puml_path, png_path):
    """Render a single diagram using available method."""
    # Ensure output dir exists
    os.makedirs(os.path.dirname(png_path) or '.', exist_ok=True)

    # Try Java + plantuml.jar first (best quality, local)
    if render_with_java(puml_path, png_path):
        return True

    # Try pythonplantuml
    if render_with_pythonplantuml(puml_path, png_path):
        return True

    # Try plantuml package (web server)
    if render_with_plantuml_package(puml_path, png_path):
        return True

    return False


def main():
    print("Generating patent figures from PlantUML...")

    if not os.path.isdir(DIAGRAMS_DIR):
        print(f"Error: diagrams directory not found: {DIAGRAMS_DIR}", file=sys.stderr)
        sys.exit(1)

    ok = 0
    for puml_name, png_name in FIGURES:
        puml_path = os.path.join(DIAGRAMS_DIR, puml_name)
        png_path = os.path.join(OUTPUT_DIR, png_name)

        if not os.path.isfile(puml_path):
            print(f"  SKIP {png_name}: source not found")
            continue

        if render_diagram(puml_path, png_path):
            print(f"  {png_name}")
            ok += 1
        else:
            print(f"  FAIL {png_name}: no renderer available", file=sys.stderr)
            print("  Install: Java + plantuml.jar, or: pip install plantuml", file=sys.stderr)

    print(f"Done! {ok}/{len(FIGURES)} figures saved to {OUTPUT_DIR}")
    if ok < len(FIGURES):
        sys.exit(1)


if __name__ == '__main__':
    main()
