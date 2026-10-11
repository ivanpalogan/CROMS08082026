"""Builds "Kiosk User's View.docx" in the layout of the office's "lastonetry.docx" (User's View N / title /
Data Environment picture, then Data Entry picture + Data Description), for the public kiosk screens.

  python build_kiosk_userview.py <template.docx> <out.docx>

The template's styles, page setup (US Letter landscape, 0.7 in margins) and fonts are reused; only the body
and the pictures are replaced. Screenshots come from kshots/final (see kiosk_post.py); the diagrams are drawn
by er_chen.py from the columns each kiosk screen really writes (KioskCore.Submit, SaveCtcRequest,
SaveBreqsRequest).
"""
import os, re, sys, zipfile, io
from xml.sax.saxutils import escape
from PIL import Image
from er_chen import draw_er

HERE = os.path.dirname(os.path.abspath(__file__))
SHOTS = os.path.join(HERE, 'kshots', 'final')
ENVDIR = os.path.join(HERE, 'kshots', 'env')
os.makedirs(ENVDIR, exist_ok=True)

PK, FK, N = 'pk', 'fk', ''
def A(*labels):
    return [(l, N) for l in labels]

TICKET_ID = [('id', PK)]

VIEWS = [
    dict(title='Welcome', shot='k1_welcome',
         env=([('WINDOWS', [('id', PK), ('window_name', N), ('status', N), ('current_operator', N), ('operator_name', N), ('last_heartbeat', N)])], None),
         desc=("The Welcome screen is the first thing a client sees on the kiosk. It shows the Local Civil Registry Office of "
               "Peñablanca, Cagayan, and the \"Touch anywhere to start\" button. No data is typed here. The screen only checks "
               "the windows table to know if the office is open: the office is open when at least one window has an operator "
               "signed in (current_operator) and its last_heartbeat is recent. When the office is open, the screen says "
               "\"The office is open. Tap the screen to begin.\" and touching it opens the service selection. When no window "
               "is open, the kiosk says the office is currently closed and does not issue a queue number.")),
    dict(title='Select Services', shot='k2_services',
         env=([('QUEUE_TICKETS', [('id', PK), ('ticket_code', N)]),
               ('QUEUE_TICKET_SERVICES', [('id', PK), ('ticket_id', FK), ('service_code', N), ('service_label', N)])], ('has', '1', 'N')),
         desc=("The client taps one or more services on the Request a Service screen (step 1 of 3). The services are grouped "
               "as Registration (Birth, Marriage, Death), Marriage & Family (Marriage Application, Legitimation, Legitimation "
               "RA-9255), Copies & Pick-up (Certified True Copy, PSA Copy, Release & Claim) and Petitions & Legal (Petition, "
               "Supplemental Report, Legal Instruments, Court Order). A card is available only when an open window handles "
               "that service, and a client may choose several cards for one visit. Nothing is saved yet: each chosen card "
               "becomes one row of queue_ticket_services (service_code and service_label) when the client confirms the "
               "request on the Review step, and all rows share one queue ticket. The next screen depends on the choice: "
               "Certified True Copy and PSA Copy ask for the document details first, the other services go straight to the "
               "personal information.")),
    dict(title='Certified True Copy Request', shot='k3_ctc',
         env=([('QUEUE_TICKETS', [('id', PK), ('document_type', N)]),
               ('KIOSK_CTC_INTAKE', [('id', PK), ('queue_ticket_id', FK), ('doc_type', N), ('copies', N), ('purpose', N), ('relationship', N),
                                     ('registry_no', N), ('owner_first', N), ('owner_middle', N), ('owner_last', N), ('owner_suffix', N),
                                     ('spouse_first', N), ('spouse_middle', N), ('spouse_last', N), ('spouse_suffix', N),
                                     ('event_date', N), ('event_city', N), ('event_province', N), ('remarks', N)])], ('requests', '1', 'N')),
         desc=("When the client chooses Certified True Copy (CTC), the kiosk asks which record is wanted so the staff can find it "
               "in the registry books. The first part, \"What you need\", is for the document type (Birth, Marriage or Death), "
               "the number of copies, and the purpose of the request. The second part, \"Whose record\", is for the registry "
               "number if the client knows it, the relationship to the record owner, and the first name, middle name, last "
               "name and suffix of the owner. For a Marriage the husband and the wife are asked separately, and the labels "
               "follow the document (the child, the husband and wife, or the deceased). The date, province and "
               "city or municipality of the birth, marriage or death are chosen from the Philippine place lists, and the city "
               "list only shows the cities of the chosen province. An optional line holds anything that helps to find the "
               "record. Only the document type, the first and last name, the province and the city are required. The answers "
               "are saved as one row of kiosk_ctc_intake linked to the queue ticket, with the status Requested, so staff "
               "see the request when the client is called.")),
    dict(title='PSA Copy Request (BREQS)', shot='k4_psa',
         env=([('QUEUE_TICKETS', [('id', PK), ('document_type', N)]),
               ('PSA_COPY_REQUESTS', [('id', PK), ('queue_ticket_id', FK), ('doc_type', N), ('copies', N), ('purpose', N), ('relationship', N),
                                      ('valid_id_type', N), ('valid_id_no', N), ('owner_first', N), ('owner_middle', N), ('owner_last', N),
                                      ('spouse_first', N), ('spouse_middle', N), ('spouse_last', N), ('event_date', N), ('event_city', N),
                                      ('event_province', N), ('father_name', N), ('mother_maiden_name', N)])], ('requests', '1', 'N')),
         desc=("A client who needs a copy issued by PSA chooses PSA Copy (BREQS), and the screen has two cards: PSA Document and "
               "Details of the Document. The first card is for choosing the certificate (Birth, Marriage or Death), the number "
               "of copies, the purpose, the relationship to the person on the document, and the valid ID the client will "
               "present, with its ID number. The second card is for the details that let PSA find the record: the name on the "
               "certificate (first, middle and last name, plus the spouse for a marriage), the date of the event, the city or "
               "municipality and the province, and, for a birth, the father's full name and the mother's full maiden name. "
               "Only what the client knows has to be filled in. The name and contact number of the person asking are taken "
               "from the next step, the personal information. The request is saved as one row of psa_copy_requests linked to "
               "the queue ticket, with the status Requested and a BREQS request number, so the staff can continue it at the "
               "PSA Copies desk.")),
    dict(title='Personal Information and Photo', shot='k5_personal',
         env=([('QUEUE_TICKETS', [('id', PK), ('full_name', N), ('contact_no', N), ('priority', N), ('valid_id_type', N), ('id_image', N)])], None),
         desc=("Every client gives the personal information on step 2, Personal Info & Photo. The left card is for the first name, "
               "middle name and last name, which are saved together as the full name of the queue ticket, and the contact "
               "number, which is typed in the +63 format. The Priority Lane buttons (Senior Citizen, PWD, Pregnant) are tapped "
               "only when they apply, and they place the ticket in the priority lane. The optional Valid ID part is for the type "
               "of ID the client will bring; the ID number is kept only for a PSA copy request. The right card shows the live "
               "camera, and the \"Capture Photo\" button takes the photo of the client, which is saved with the ticket "
               "(id_image) so the staff can see who is being served. The first name and the last name are required.")),
    dict(title='Personal Information - Marriage Registration', shot='k6_personal_marriage_reg',
         env=([('QUEUE_TICKETS', [('id', PK), ('full_name', N), ('contact_no', N), ('priority', N), ('valid_id_type', N), ('id_image', N),
                                  ('submitted_by', N), ('submitted_by_org', N)])], None),
         desc=("When Marriage Registration is chosen, the same Personal Information card is used, but its heading becomes \"Your "
               "Information (the person submitting)\", because the person at the kiosk is the one handing in the Certificate of "
               "Marriage. An extra required question, \"Who is submitting the Certificate of Marriage?\", has four buttons: "
               "Solemnizing Officer, Husband, Wife, and Others (please specify). When Others is tapped, a box opens for the "
               "client to write who they are (for example a relative or the office that sent them), and it is required. The "
               "answer is saved on the queue ticket (submitted_by and submitted_by_org) and is shown to the staff when they "
               "open the Marriage Registration, so the staff do not need to ask again. The couple themselves are not typed at "
               "the kiosk, because they are on the certificate that the staff will scan.")),
    dict(title='Personal Information - Marriage Application', shot='k7_personal_marriage_app',
         env=([('QUEUE_TICKETS', [('id', PK), ('full_name', N), ('spouse_full_name', N), ('app_h_first', N), ('app_h_middle', N), ('app_h_last', N),
                                  ('app_w_first', N), ('app_w_middle', N), ('app_w_last', N), ('contact_no', N), ('priority', N),
                                  ('valid_id_type', N), ('id_image', N), ('spouse_image', N)])], None),
         desc=("For a Marriage Application the card is called \"Couple's Information\" and has two columns, Husband and Wife, "
               "each with a first name, middle name and last name (the first and last name are required). One contact number, "
               "the priority lane and the optional valid ID are shared by the couple. The camera card has a Husband tab and a "
               "Wife tab, so each applicant has their own photo. The two names are saved on the queue ticket as the full name "
               "and the spouse full name, and, as separate first, middle and last name cells (app_h_ and app_w_), so the "
               "staff's Marriage License Application (Form 90) opens with the names already filled in. The two photos are "
               "saved as id_image and spouse_image.")),
    dict(title='Review and Confirm', shot='k8_review',
         env=([('QUEUE_TICKETS', [('id', PK), ('ticket_code', N), ('number_queue', N), ('date', N), ('time', N), ('status', N),
                                  ('document_type', N), ('purpose', N), ('type_label', N), ('priority', N)]),
               ('QUEUE_TICKET_SERVICES', [('id', PK), ('ticket_id', FK), ('service_code', N), ('service_label', N)])], ('has', '1', 'N')),
         desc=("The last step, Review Your Request, shows everything the client entered so it can be checked before a number is "
               "given: the client's name and contact number, the services selected, the certified true copy or PSA copy "
               "summary when one was chosen, and the priority lane. Nothing can be edited here; the Back button returns to "
               "the earlier steps, and \"Add Another Transaction\" returns to the service cards for one more service on the "
               "same ticket. The \"Confirm & Print Ticket\" button creates the queue ticket: the next queue number "
               "(ticket_code, such as Q-001, and number_queue), the date and time, the status Waiting, the document type, a "
               "one-line summary of the request (purpose), the list of services (type_label) and the priority lane. One row "
               "of queue_ticket_services is saved for each service, and the CTC or PSA request is saved with it. The kiosk "
               "then prints the queue ticket and shows the number, and the client waits to be called at a window.")),
]

# ---------------------------------------------------------------------------------------- docx helpers
EMU = 914400
RPR = lambda sz, b: ('<w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>%s<w:sz w:val="%d"/></w:rPr>'
                     % ('<w:b/>' if b else '<w:b w:val="0"/>', sz))

def para(text, sz, bold, after, jc=None):
    return ('<w:p><w:pPr><w:spacing w:after="%d"/>%s</w:pPr><w:r>%s<w:t xml:space="preserve">%s</w:t></w:r></w:p>'
            % (after, '<w:jc w:val="%s"/>' % jc if jc else '', RPR(sz, bold), escape(text)))

def pagebreak():
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'

def picture(rid, idn, name, cx, cy):
    return ('<w:p><w:pPr><w:jc w:val="center"/></w:pPr><w:r><w:drawing><wp:inline xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" '
            'xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><wp:extent cx="%d" cy="%d"/><wp:docPr id="%d" name="Picture %d" descr="%s"/>'
            '<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect="1"/></wp:cNvGraphicFramePr><a:graphic><a:graphicData '
            'uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="0" name="%s"/><pic:cNvPicPr/></pic:nvPicPr>'
            '<pic:blipFill><a:blip r:embed="%s"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/>'
            '<a:ext cx="%d" cy="%d"/></a:xfrm><a:prstGeom prst="rect"/></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>'
            % (cx, cy, idn, idn, escape(name, {'"': '&quot;'}), escape(name, {'"': '&quot;'}), rid, cx, cy))

def fit(w, h, max_w, max_h):
    s = min(max_w / w, max_h / h)
    return int(w * s), int(h * s)


def main(template, out):
    zin = zipfile.ZipFile(template)
    doc = zin.read('word/document.xml').decode('utf8')
    head = doc[:doc.index('<w:body>') + len('<w:body>')]
    sect = re.findall(r'<w:sectPr.*?</w:sectPr>', doc)[-1]
    rels = zin.read('word/_rels/document.xml.rels').decode('utf8')
    rels = re.sub(r'<Relationship [^>]*relationships/image"[^>]*/>', '', rels)

    body, new_rels, media = [], [], {}
    pid = 0
    MAXW = 8595360                     # 9.4 in, same as the template's wide pictures
    for i, v in enumerate(VIEWS, 1):
        # ---- Data Environment picture
        env_png = os.path.join(ENVDIR, '%d %s.png' % (i, v['title']))
        ents, rel = v['env']
        img = draw_er(ents, rel); img.save(env_png)
        ew, eh = fit(img.width, img.height, MAXW, int(5.0 * EMU))
        # ---- screenshot
        shot_png = os.path.join(SHOTS, v['shot'] + '.png')
        sim = Image.open(shot_png)
        sw, sh = fit(sim.width, sim.height, int(8.2 * EMU), int(4.15 * EMU))

        rid_env, rid_shot = 'rId%d' % (100 + 2 * i), 'rId%d' % (101 + 2 * i)
        media['word/media/env%d.png' % i] = open(env_png, 'rb').read()
        media['word/media/shot%d.png' % i] = open(shot_png, 'rb').read()
        new_rels.append('<Relationship Id="%s" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/env%d.png"/>' % (rid_env, i))
        new_rels.append('<Relationship Id="%s" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/shot%d.png"/>' % (rid_shot, i))

        body.append(para("Kiosk User's View %d" % i, 28, True, 40))
        body.append(para(v['title'], 26, True, 120))
        body.append(para('Data Environment', 24, True, 120))
        pid += 1; body.append(picture(rid_env, pid, '%d %s data environment' % (i, v['title']), ew, eh))
        body.append(pagebreak())
        body.append(para("Kiosk User's View %d - %s" % (i, v['title']), 24, True, 40))
        body.append(para('Data Entry', 24, True, 120))
        pid += 1; body.append(picture(rid_shot, pid, '%d %s kiosk screen' % (i, v['title']), sw, sh))
        body.append(para('Data Description', 24, True, 120))
        body.append(para(v['desc'], 24, False, 120, 'both'))
        if i != len(VIEWS):
            body.append(pagebreak())

    new_doc = head + ''.join(body) + sect + '</w:body></w:document>'
    rels = rels.replace('</Relationships>', ''.join(new_rels) + '</Relationships>')

    with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as zout:
        for item in zin.infolist():
            if item.filename.startswith('word/media/'):
                continue
            data = zin.read(item.filename)
            if item.filename == 'word/document.xml':
                data = new_doc.encode('utf8')
            elif item.filename == 'word/_rels/document.xml.rels':
                data = rels.encode('utf8')
            zout.writestr(item, data)
        for name, data in media.items():
            zout.writestr(name, data)
    print('wrote', out, len(VIEWS), 'views')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
