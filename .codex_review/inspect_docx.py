import json
import sys
import zipfile
import xml.etree.ElementTree as ET

W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
NS = {"w": W, "r": R}


def q(name):
    return f"{{{W}}}{name}"


def run_record(run):
    pieces = []
    for node in run.iter():
        if node.tag == q("t"):
            pieces.append(node.text or "")
        elif node.tag == q("tab"):
            pieces.append("\\t")
        elif node.tag in (q("br"), q("cr")):
            pieces.append("\\n")
    rpr = run.find("w:rPr", NS)
    underline = None
    if rpr is not None:
        u = rpr.find("w:u", NS)
        if u is not None:
            underline = u.get(q("val"), "single")
    return {"text": "".join(pieces), "underline": underline}


def paragraph_record(p):
    ppr = p.find("w:pPr", NS)
    align = None
    border = None
    if ppr is not None:
        jc = ppr.find("w:jc", NS)
        if jc is not None:
            align = jc.get(q("val"))
        pbdr = ppr.find("w:pBdr", NS)
        if pbdr is not None:
            border = [child.tag.rsplit("}", 1)[-1] for child in list(pbdr)]
    runs = [run_record(r) for r in p.findall(".//w:r", NS)]
    return {"align": align, "border": border, "runs": runs}


def table_record(tbl):
    rows = []
    for tr in tbl.findall("w:tr", NS):
        cells = []
        for tc in tr.findall("w:tc", NS):
            cells.append([
                paragraph_record(p) for p in tc.findall("w:p", NS)
            ])
        rows.append(cells)
    return rows


def inspect(path):
    with zipfile.ZipFile(path) as zf:
        root = ET.fromstring(zf.read("word/document.xml"))
        body = root.find("w:body", NS)
        blocks = []
        for child in list(body):
            if child.tag == q("p"):
                blocks.append({"type": "paragraph", "value": paragraph_record(child)})
            elif child.tag == q("tbl"):
                blocks.append({"type": "table", "value": table_record(child)})
            elif child.tag == q("sectPr"):
                size = child.find("w:pgSz", NS)
                blocks.append({
                    "type": "section",
                    "value": None if size is None else {
                        "width_twips": size.get(q("w")),
                        "height_twips": size.get(q("h")),
                        "orientation": size.get(q("orient"), "portrait"),
                    },
                })
        extras = {}
        for name in sorted(zf.namelist()):
            if name.startswith("word/header") and name.endswith(".xml") or name.startswith("word/footer") and name.endswith(".xml"):
                eroot = ET.fromstring(zf.read(name))
                extras[name] = [paragraph_record(p) for p in eroot.findall(".//w:p", NS)]
        return {"path": path, "blocks": blocks, "headers_footers": extras}


print(json.dumps(inspect(sys.argv[1]), ensure_ascii=False, indent=2))
