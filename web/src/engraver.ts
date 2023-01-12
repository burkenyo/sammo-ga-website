import { Accidental, Barline, Formatter, Renderer, Stave, StaveNote, TextNote, Voice, Flow, Stem, type RenderContext } from "vexflow";

const CLEF_OFFSET = 40;
const NOTE_WIDTH = 24;
const ACCIDENTAL_WIDTH = 12;
const STAFF_HEIGHT = 90;
const CANVAS_PADDING = 30;
const TERMINAL_BAR_LINE_WIDTH = 5;

export class Engraver {
  readonly #context: RenderContext;

  constructor(engraveArea: HTMLDivElement) {
    this.#context = new Renderer(engraveArea, Renderer.Backends.SVG).getContext();
  }

  drawNotes(notes: string[], offset: number, workableWidth: number) {
    let i = 0;
    const staves: {
      notes: StaveNote[];
      width: number;
      offset: number;
    }[] = [];

    while (i < notes.length) {
      const vexNotes = [];
      let width = CLEF_OFFSET;
      const staffOffsetNum = offset + i + 1;
      while (i < notes.length) {
        width += NOTE_WIDTH;
        const accidental = notes[i].charAt(1) == "#";
        if (accidental) {
          width += ACCIDENTAL_WIDTH;
        }
        if (width > workableWidth) {
          width = workableWidth;
          break;
        }

        const note = new StaveNote({ keys: [notes[i]], duration: "4", stem_direction: Stem.DOWN });
        if (accidental) {
          note.addModifier(new Accidental("#"));
        }
        vexNotes.push(note);

        i++;
      }
      staves.push({ notes: vexNotes, width: width, offset: staffOffsetNum });
    }

    const lastStave = staves[staves.length - 1];
    if (workableWidth - lastStave.width < 2 * (NOTE_WIDTH + ACCIDENTAL_WIDTH)) {
      lastStave.width = workableWidth - TERMINAL_BAR_LINE_WIDTH;
    } else if (staves.length > 1 && lastStave.width < 2 * (NOTE_WIDTH + ACCIDENTAL_WIDTH) + CLEF_OFFSET) {
      const newLastStave = staves[staves.length - 2];
      newLastStave.notes = newLastStave.notes.concat(lastStave.notes);
      newLastStave.width = workableWidth - TERMINAL_BAR_LINE_WIDTH;
      staves.pop();
    }

    this.#context.clear();
    this.#context.resize(workableWidth, staves.length * STAFF_HEIGHT + CANVAS_PADDING);

    for (i = 0; i < staves.length; i++) {
      const stave = new Stave(0, i * STAFF_HEIGHT, i + 1 == staves.length ? staves[i].width + TERMINAL_BAR_LINE_WIDTH : staves[i].width);
      if (i + 1 == staves.length) {
        stave.setEndBarType(Barline.type.END);
      } else {
        stave.setEndBarType(Barline.type.NONE);
      }
      stave.addClef("treble").setContext(this.#context).draw();

      const text = new TextNote({ text: String(staves[i].offset), duration: "q" }).setLine(2.6)
        .setStave(stave).setJustification(TextNote.Justification.CENTER);

      const voice = new Voice({ num_beats: staves[i].notes.length, beat_value: 4, resolution: Flow.RESOLUTION });
      const voice2 = new Voice({ num_beats: staves[i].notes.length, beat_value: 4, resolution: Flow.RESOLUTION });
      voice.addTickables(staves[i].notes);
      voice2.addTickables([text]);
      voice2.setStrict(false);

      new Formatter().joinVoices([voice, voice2]).format([voice, voice2], staves[i].width - CLEF_OFFSET);
      voice.draw(this.#context, stave);
      text.setContext(this.#context).draw();
    }
  }
}
