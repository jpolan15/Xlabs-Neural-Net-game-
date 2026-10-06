# Chamber 01 art bible

One page, distilled from the Chamber 01 look contract. This page does not add decisions. Colors live in `Assets/Materials/AGENTS.md`. Do not copy hex values out of that file.

## Style

A worn but dignified research-ship bridge. Clean, stylized low-poly shapes. Strong value zoning. Small areas of saturated state color. The eye lands on the console. Silhouettes say what an object is.

## Color

Saturated cyan, amber, and red appear only on state: lamps, wires, the FIRE node, accent strips, alert glyphs.

- Cyan means positive, active, or handled.
- Amber means not yet, wrong, or crisis. It is a negative outcome, not a negative number. A human has not confirmed that reading.
- Slate is neutral: the hold side, a zero-weight wire, an inactive node.
- Red is overload only. Do not use red for FIRE.
- A negative weight is a dashed cyan wire with a minus glyph. A positive weight is a solid cyan wire. Zero is a hairline slate wire.
- No other saturated hue. Labels are off-white.
- Rock and ice are told apart by icon and shape, not by color.

## Text

Size by angle: height in meters is distance times the tangent of the angle. Body text is at least the body angle in `Tools/chamber01-thresholds.json`. Headings use the heading angle. Icons use the icon angle. Those numbers stay advisory until a human calibrates them.

Billboards use `Quaternion.LookRotation(textPos - eyePos)`. Do not use `LookAt` on the camera.

One prompt panel. At most 12 words. Labels are at most 6 words, or they are icons. Keep text inside the text cone in that same thresholds file, around the seated gaze toward the console.

The formula stays behind "Show the math", off by default.

## Materials and value

Visible architecture is chamfered modules at uniform scale, with the atlas visible. Four materials: painted hull, dark panel, viewport glass, and one emissive accent whose color is state.

Hull smoothness starts at 0.25. Metallic stays at or below 0.3.

`variation-b` and `variation-c` are not in the Kenney folder. Tint `_BaseColor` on `Mat_Kenney_SpaceStation` and keep `variation-a.png`. Do not upscale that texture.

Value order, darkest to brightest: ceiling, floor, walls, console surround, then state-color emissives. The builder places `ValueProbe_Ceiling`, `ValueProbe_Floor`, and `ValueProbe_Wall` so a capture can measure this.

## What this page does not decide

Headset angles, glare limits, and whether amber means a negative number are human calls. They are listed in the task file under open questions and not measured.
