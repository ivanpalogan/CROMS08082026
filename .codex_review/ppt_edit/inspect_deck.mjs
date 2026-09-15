import fs from "node:fs/promises";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const sourcePath = "C:/Users/ivan palogan/Downloads/CROMS_Presentation.pptx";
const buildDir = "C:/Users/ivan palogan/OneDrive/Documents/Capstone/CROMS/.codex_review/ppt_edit";

const presentation = await PresentationFile.importPptx(await FileBlob.load(sourcePath));
const snapshot = await presentation.inspect({
  kind: "deck,slide,textbox,shape,image,layout,notes",
  include: "id,slide,name,title,text,textPreview,bbox,bboxUnit,isPlaceholder,placeholders",
  maxChars: 50000,
});

await fs.writeFile(`${buildDir}/deck-inspect.ndjson`, snapshot.ndjson, "utf8");
const summary = {
  slideCount: presentation.slides.items.length,
  slideSize: presentation.slideSize,
  masters: presentation.masters.items.map((item) => ({ id: item.id, name: item.name })),
  layouts: presentation.layouts.items.map((item) => ({
    id: item.id,
    name: item.name,
    placeholders: item.placeholders.summary(),
  })),
};
console.log(JSON.stringify(summary, null, 2));
console.log(snapshot.ndjson);
