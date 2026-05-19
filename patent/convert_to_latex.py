"""
Convert patent markdown files to standalone LaTeX documents.
Output: referat.tex, opys_vynakhodu.tex, formula_vynakhodu.tex
"""

import os
import re
import pypandoc

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
os.chdir(SCRIPT_DIR)

PREAMBLE = r"""\documentclass[12pt,a4paper]{article}
\usepackage[utf8]{inputenc}
\usepackage[T2A,T1]{fontenc}
\usepackage[ukrainian]{babel}
\usepackage{amsmath,amssymb}
\usepackage{booktabs}
\usepackage{longtable}
\usepackage{array}
\usepackage{calc}
\usepackage{geometry}
\geometry{margin=2.5cm}

\begin{document}
"""

POSTAMBLE = r"""
\end{document}
"""

def convert(md_name, tex_name):
    md_path = os.path.join(SCRIPT_DIR, md_name)
    tex_path = os.path.join(SCRIPT_DIR, tex_name)
    if not os.path.isfile(md_path):
        return False
    body = pypandoc.convert_file(md_path, 'latex', extra_args=['--wrap=none'])
    # Fix pandoc artifacts
    body = re.sub(r'\\real\{([^}]+)\}', r'\1', body)
    # Simplify longtable column specs for compatibility: (linewidth - N tabcolsep) * X -> X\linewidth
    body = re.sub(r'\(\\linewidth - \d+\\tabcolsep\) \* ([\d.]+)', r'\1\\linewidth', body)
    body = re.sub(r'\\label\{ux[0-9a-f]+\}', '', body)
    with open(tex_path, 'w', encoding='utf-8') as f:
        f.write(PREAMBLE)
        f.write(body)
        f.write(POSTAMBLE)
    return True

def main():
    for md, tex in [('referat.md', 'referat.tex'), ('opys_vynakhodu.md', 'opys_vynakhodu.tex'), ('formula_vynakhodu.md', 'formula_vynakhodu.tex')]:
        if convert(md, tex):
            print(f"OK {tex}")
        else:
            print(f"SKIP {tex}")

if __name__ == '__main__':
    main()
