"""
Generate PDF files for patent application.
Converts: opys_vynakhodu.md, formula_vynakhodu.md, referat.md -> PDF.
Creates: zaiava_template.md (for manual completion) and kreslennya.pdf (figures).
Requires: pypandoc, playwright, markdown
"""

import os
import subprocess
import sys

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
os.chdir(SCRIPT_DIR)

DOCUMENTS = [
    ('opys_vynakhodu.md', '01_opys_vynakhodu.pdf', 'Опис винаходу'),
    ('formula_vynakhodu.md', '02_formula_vynakhodu.pdf', 'Формула винаходу'),
    ('referat.md', '03_referat.pdf', 'Реферат'),
]


def md_to_html(md_path, html_path):
    """Convert markdown to HTML with math support."""
    import pypandoc
    pypandoc.convert_file(
        md_path,
        'html5',
        outputfile=html_path,
        extra_args=[
            '--standalone',
            '--mathjax',
            '-V', 'lang=uk',
            '--metadata', 'title=',
        ]
    )


def html_to_pdf(html_path, pdf_path):
    """Convert HTML to PDF using Playwright."""
    from playwright.sync_api import sync_playwright

    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()
        page.goto(f'file://{os.path.abspath(html_path)}', wait_until='networkidle')
        page.pdf(path=pdf_path, format='A4', margin={'top': '20mm', 'bottom': '20mm', 'left': '20mm', 'right': '20mm'})
        browser.close()


def convert_md_to_pdf(md_name, pdf_name):
    """Convert single markdown file to PDF."""
    md_path = os.path.join(SCRIPT_DIR, md_name)
    pdf_path = os.path.join(SCRIPT_DIR, pdf_name)
    html_path = os.path.join(SCRIPT_DIR, md_name.replace('.md', '_temp.html'))

    if not os.path.isfile(md_path):
        print(f"  SKIP {pdf_name}: {md_name} not found")
        return False

    try:
        md_to_html(md_path, html_path)
        html_to_pdf(html_path, pdf_path)
        os.remove(html_path)
        print(f"  OK {pdf_name}")
        return True
    except Exception as e:
        print(f"  FAIL {pdf_name}: {e}", file=sys.stderr)
        if os.path.isfile(html_path):
            os.remove(html_path)
        return False


def create_zaiava_template():
    """Create application form template (markdown) - user fills and converts to PDF manually."""
    template = """# ЗАЯВА про державну реєстрацію винаходу (корисної моделі)

*(Заповнити згідно з офіційною формою НОІВ)*

**Посилання на форму:** https://nipo.gov.ua/wp-content/uploads/2025/05/application-reg-invention-utility-model-02052025.docx

**Приклад заповнення:** https://nipo.gov.ua/wp-content/uploads/2024/11/PRYKLAD_zaiava_reiestratsia_VYNAKHID_KM-new-web.pdf

---

## Дані для заповнення

**Назва винаходу:** Система та спосіб обробки багатоканальних EEG-даних для обчислення показника обчислювальної ментальної енергії (CME) із застосуванням квантового обчислювального модуля та метаевристичної оптимізації.

**МПК:** A61B 5/372, G06N 10/00, G16H 50/20

**Заявник:** Національний технічний університет України "Київський політехнічний інститут імені Ігоря Сікорського"
Адреса: 03056, м. Київ, проспект Перемоги, 37

**Винахідники:** *(заповнити)*

**Адреса для листування:** *(заповнити)*

---

*Заяву потрібно заповнити у форматі DOCX (завантажити з сайту НОІВ), підписати та зберегти як PDF.*
"""
    path = os.path.join(SCRIPT_DIR, 'zaiava_template.md')
    with open(path, 'w', encoding='utf-8') as f:
        f.write(template)
    print("  OK zaiava_template.md")


def create_kreslennya_pdf():
    """Create PDF with figures (if PNG files exist)."""
    figures = ['fig1_system_structure.png', 'fig2_online_cme_flow.png', 'fig3_metaheuristic_loop.png', 'fig4_spectral_features.png']
    existing = [f for f in figures if os.path.isfile(os.path.join(SCRIPT_DIR, f))]
    if not existing:
        print("  SKIP 04_kreslennya.pdf: run 'python generate_figures.py' first to create PNG figures")
        return False

    try:
        from reportlab.lib.pagesizes import A4
        from reportlab.pdfgen import canvas
        from reportlab.lib.units import mm

        pdf_path = os.path.join(SCRIPT_DIR, '04_kreslennya.pdf')
        c = canvas.Canvas(pdf_path, pagesize=A4)
        w, h = A4

        for i, fig in enumerate(existing, 1):
            if i > 1:
                c.showPage()
            img_path = os.path.join(SCRIPT_DIR, fig)
            c.setFont("Helvetica", 10)
            c.drawString(w - 60*mm, h - 15*mm, f"Фіг. {i}")
            c.drawImage(img_path, 20*mm, 30*mm, width=170*mm, height=220*mm, preserveAspectRatio=True, anchor='c')
        c.save()
        print(f"  OK 04_kreslennya.pdf ({len(existing)} figures)")
        return True
    except ImportError:
        print("  SKIP 04_kreslennya.pdf: pip install reportlab")
        return False
    except Exception as e:
        print(f"  FAIL 04_kreslennya.pdf: {e}", file=sys.stderr)
        return False


def main():
    print("Generating patent PDFs...")

    # 1. Create zaiava template
    create_zaiava_template()

    # 2. Convert main documents
    for md_name, pdf_name, _ in DOCUMENTS:
        convert_md_to_pdf(md_name, pdf_name)

    # 3. Create kreslennya PDF
    create_kreslennya_pdf()

    print("\nDone! PDF files in:", SCRIPT_DIR)
    print("\nPatent application documents:")
    print("  1. Zaiava - fill form from NOIV, save as PDF")
    print("  2. 01_opys_vynakhodu.pdf - description")
    print("  3. 02_formula_vynakhodu.pdf - claims")
    print("  4. 04_kreslennya.pdf - drawings (Fig 1-4)")
    print("  5. 03_referat.pdf - abstract")


if __name__ == '__main__':
    main()
