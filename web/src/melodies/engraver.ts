// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

import { dynamicImport } from "@shared/utils";
// only types can be “imported” directly
import type { RenderContext, StaveNote as StaveNoteType } from "vexflow";

// import vexflow code dynamically
const vexflowImport = dynamicImport<typeof import("vexflow")>("https://cdn.jsdelivr.net/npm/vexflow@5.0.0/+esm");

const CLEF_OFFSET = 40;
const NOTE_WIDTH = 24;
const ACCIDENTAL_WIDTH = 12;
const STAFF_HEIGHT = 90;
const CANVAS_PADDING = 30;
const TERMINAL_BAR_LINE_WIDTH = 5;

export interface Engraver {
  drawNotes(notes: string[], offset: number, workableWidth: number): void;
}

export async function useEngraver(element: HTMLDivElement): Promise<Engraver> {
  const { Accidental, Barline, Formatter, Renderer, Stave, StaveNote, TextNote, Voice, VexFlow, Stem }
    = await vexflowImport.value;

  class Engraver {
    readonly #context: RenderContext;

    constructor(element: HTMLDivElement) {
      this.#context = new Renderer(element, Renderer.Backends.SVG).getContext();
    }

    drawNotes(notes: string[], offset: number, workableWidth: number): void {
      let i = 0;
      const staves: {
        notes: StaveNoteType[];
        width: number;
        offset: number;
      }[] = [];

      while (i < notes.length) {
        const vexNotes = [];
        let width = CLEF_OFFSET;
        const staffOffsetNum = offset + i + 1;
        while (i < notes.length) {
          const note = notes[i]!;
          width += NOTE_WIDTH;
          const accidental = note.charAt(1) == "#";
          if (accidental) {
            width += ACCIDENTAL_WIDTH;
          }
          if (width > workableWidth) {
            width = workableWidth;
            break;
          }

          const vexNote = new StaveNote({ keys: [note], duration: "4", stemDirection: Stem.DOWN });
          if (accidental) {
            vexNote.addModifier(new Accidental("#"));
          }
          vexNotes.push(vexNote);

          i++;
        }
        staves.push({ notes: vexNotes, width: width, offset: staffOffsetNum });
      }

      const lastStave = staves[staves.length - 1]!;
      if (workableWidth - lastStave.width < 2 * (NOTE_WIDTH + ACCIDENTAL_WIDTH)) {
        lastStave.width = workableWidth - TERMINAL_BAR_LINE_WIDTH;
      } else if (staves.length > 1 && lastStave.width < 2 * (NOTE_WIDTH + ACCIDENTAL_WIDTH) + CLEF_OFFSET) {
        const newLastStave = staves[staves.length - 2]!;
        newLastStave.notes = newLastStave.notes.concat(lastStave.notes);
        newLastStave.width = workableWidth - TERMINAL_BAR_LINE_WIDTH;
        staves.pop();
      }

      this.#context.clear();
      this.#context.resize(workableWidth, staves.length * STAFF_HEIGHT + CANVAS_PADDING);

      for (i = 0; i < staves.length; i++) {
        const staff = staves[i]!;
        const vefStaff = new Stave(0, i * STAFF_HEIGHT, i + 1 == staves.length ? staff.width + TERMINAL_BAR_LINE_WIDTH : staff.width);
        if (i + 1 == staves.length) {
          vefStaff.setEndBarType(Barline.type.END);
        } else {
          vefStaff.setEndBarType(Barline.type.NONE);
        }
        vefStaff.addClef("treble").setContext(this.#context).draw();

        const text = new TextNote({ text: String(staff.offset), duration: "q" }).setLine(2.6)
          .setStave(vefStaff).setJustification(TextNote.Justification.CENTER);

        const voice = new Voice({ numBeats: staff.notes.length, beatValue: 4, resolution: VexFlow.RESOLUTION });
        const voice2 = new Voice({ numBeats: staff.notes.length, beatValue: 4, resolution: VexFlow.RESOLUTION });
        voice.addTickables(staff.notes);
        voice2.addTickables([text]);
        voice2.setStrict(false);

        new Formatter().joinVoices([voice, voice2]).format([voice, voice2], staff.width - CLEF_OFFSET);
        voice.draw(this.#context, vefStaff);
        text.setContext(this.#context).draw();
      }
    }
  }

  return new Engraver(element);
}
