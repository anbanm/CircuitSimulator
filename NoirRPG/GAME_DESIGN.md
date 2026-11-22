# DOORNRAND (working title) — Game Design, v3

A detective story set in a mining town over five days. Isometric, rotatable camera. Single player, one fixed main character, three to four hours.

Status: design draft, no code. This version replaces the earlier five-city, co-op, cyberpunk design. All names are placeholders.

Assumptions made in this version, to confirm:

- **Grounded, not cyberpunk.** "Keep realistic" is read as present day or a few years ahead: phones, cameras, drones, radios. No implants, no abilities, no hacking minigames.
- **Single player.** One main character means co-op is parked.
- **A fictional town.** Doornrand is invented and stands on a gold reef in South Africa. Its detail comes from how a mining town works, not from national landmarks.

---

## 1. The game in one paragraph

Ruth Mahlangu, a private investigator, drives into Doornrand on a Monday evening to meet a client. He is shot before she finds him. The town belongs to one old man who, three years ago, brought in hard people to break a strike and never got rid of them. Four of them now run the place between them. Over five days Ruth finds out who killed her client, learns what he was hiding, and decides what to tell whom. The town does the rest. On Friday the mine is to be sold, and what the town looks like that morning is the ending.

## 2. Source

The structure is taken from two Dashiell Hammett novels that are in the US public domain.

- *Red Harvest* (1929): an investigator arrives in a mining town, the man who sent for him is already dead, and he breaks the town's gangs by turning them on each other. The cost of doing that is the point of the book.
- *The Maltese Falcon* (1930): one object, and everyone who wants it tells a different story about what it is.

Only the structure is used. No text, names, scenes or dialogue are carried over. Hammett remains in copyright in the UK and EU until 2032, which is another reason to keep it to structure. Get a rights opinion before release.

## 3. Pillars

1. **What you tell people is the main verb.** The town changes because of who you informed, not who you shot.
2. **The player solves it.** The game hands out facts. Conclusions are the player's, and wrong ones play out.
3. **Violence is short, final and remembered.** Nobody is a bullet sponge, including Ruth. Everyone who dies stays dead and is counted.
4. **Five days, one town.** A small map known thoroughly, with a clock that does not wait.
5. **The basics only.** No levels, no stats, no upgrades, no loot.

## 4. Structure

Four acts across five days. Each day has three blocks: morning, afternoon, night. A block ends when the player moves time on (sleeping, waiting, travelling out to the mine) or when a scripted event closes it.

| Act | Days | Length | What happens |
|---|---|---|---|
| I. Arrival | Monday | 40 min | The client is dead. Ruth meets the town, takes a room, and is summoned to the big house, where the old man hires her to find his son's killer. |
| II. The town | Tuesday, Wednesday | 90 min | The murder is solved, rightly or wrongly. The old man tries to pay Ruth to leave. She learns about the core box and starts feeding the four powers what they want to hear. |
| III. Harvest | Thursday | 60 min | It comes apart. Ruth's best source is killed and Ruth is blamed. The town is locked down. She finds the box and reaches the old man. The heaviest stealth and action. |
| IV. Epilogue | Friday | 20 min | The morning of the sale. Ruth decides what happens to the box, then walks through what is left. No combat. |

**Leaving.** Ruth's car is parked outside the lodging house. From Tuesday onward the player can drive out at any time, and the epilogue plays for the town as it stands. Taking the old man's money on Wednesday is a complete, short, bad ending.

## 5. The town

One continuous map, about twelve places, walkable end to end in three minutes.

- **Main street:** Grace Tau's tavern and lodging house (Ruth's room, the case board, the save point), the police station, the community radio station, the general dealer, the clinic, the church.
- **The mine:** No. 4 shaft and headgear (still working), the mine office, the assay lab, the core yard (thousands of identical core boxes on racks), the security gate.
- **Where people live:** the hostel, the company houses, the informal settlement past the rail siding, the big house on the ridge.
- **The edges:** the closed No. 2 shaft, the scrap yard, the tailings dump.

The town runs on a schedule. Shifts change at 06:00, 14:00 and 22:00, and the streets, the tavern and the gate behave differently around them. Named people have places to be at each block. Power cuts happen but are not a mechanic.

## 6. People

**Ruth Mahlangu**, 44. Ten years in police commercial crime, now at a two-person firm in Johannesburg. Careful, tired, good at paperwork. Carries a licensed pistol she has drawn twice in her life.

**Daniel Brandt**, 36. The client. The owner's son, who ran the community radio station and had started broadcasting about the mine. Shot outside the station on Monday evening.

**Hendrik Brandt**, 78. Owns the mine and most of the town. Brought in Steyn to end the strike. Wants the sale to go through on Friday and wants to know who killed his son, in that order.

The four powers:

| Who | Holds | Wants |
|---|---|---|
| **Marius Steyn**, security contractor | The gate, the hostel, forty armed men | To own Brandt after the sale as he does now |
| **Abel Mokoena**, scrap dealer and gold buyer | The closed shafts and the crews who work them, many of them retrenched Doornrand miners | The untouched rock he believes the deep borehole found |
| **Grace Tau**, tavern owner and lender | Half the town's bank cards, the lodging house, every rumour | Her loans repaid before the mine changes hands |
| **Captain Sizwe Khoza**, station commander | The docket, the cells, the official version | To reach his pension without anything being written down |

Others: **Anneline Petersen**, assayer at the mine lab, who sells information to all four and knows what the core says. **Joseph Sithole**, former shaft steward, sacked after the strike, Ruth's first honest source. **Sam Nel**, technician at the radio station.

## 7. The story

**Hidden truth.** A deep borehole drilled last year was meant to prove a new reef under No. 4. It found nothing. The mine has about eighteen months left. Brandt had the results replaced, paid Anneline to sign the false assay, and is selling the mine on Friday on the strength of it. Daniel found the real core, tray 114, and its assay sheet, hid them, and hired Ruth to confirm what he had before he broadcast it.

**The object.** Nobody Ruth meets describes the box the same way.

| Who | What they say it is |
|---|---|
| Hendrik Brandt | Stolen company property, worthless to anyone else |
| Steyn | Evidence. Whoever holds it holds Brandt |
| Mokoena | A map to rock nobody has touched |
| Grace Tau | Collateral. Daniel pledged it against a loan |
| Khoza | There is no box |
| Anneline | A year and a half, then nothing |

**Who killed Daniel.** Sam Nel. He owed Grace Tau more than he could pay and had been selling her what Daniel was working on. Daniel found out on Monday afternoon. It was Daniel's own pistol, taken from the station desk. It had nothing to do with the mine. By Tuesday morning Steyn's men have dressed the scene to point at Mokoena's crews, because Steyn wants a reason to clear the closed shafts.

**Act by act.**

- **Monday.** Ruth arrives at dusk. Daniel is not at the tavern. She finds the crowd outside the radio station. Khoza gives her the official version; Joseph gives her the town; Grace gives her a room. At night she can go back into the station, which is the stealth tutorial. A car from the big house fetches her.
- **Tuesday.** The murder. Three questions on the board: who was at the station, whose gun, who gains. By nightfall Ruth names someone to Brandt and Khoza. If she names Mokoena's people, Steyn raids No. 2 shaft that night with her finding as his warrant.
- **Wednesday.** Brandt thanks her, pays her, and tells her to go. Every one of the four separately asks whether she has "it". She meets Anneline. The Tell mechanic opens fully, and the first night of faction violence follows from what she said.
- **Thursday.** Anneline is found dead in the assay lab, and Ruth was the last person seen with her. Steyn closes the roads. Ruth moves through her own town as a wanted person, finds tray 114 in the core yard hidden among thousands like it, and goes up to the big house. How many people are there, and on whose side, depends on Wednesday.
- **Friday.** The buyers' cars arrive at the mine office at 10:00. Ruth chooses what happens to the box: to the buyers, to Brandt, to Joseph and the workers, on air from Daniel's station, or into the tailings dam. None of these saves the town. The sale collapsing closes the mine now. The sale going through closes it in eighteen months with the money gone. Then she walks down the main street to her car.

**The epilogue** is playable. The street reflects who is alive, which buildings burned, what Ruth chose. Survivors will talk. The last screen gives the number of dead and the names.

## 8. Investigation

The case board is the wall of Ruth's room and a full-screen view anywhere else. There is no quest log and no objective marker.

- **Clue:** one fact, marked as *seen* (Ruth observed it), *said* (someone told her), or *written* (a document or recording, which can be false).
- **Lead:** an open question a clue raises. Leads are the to-do list and softly mark the people and places they touch.
- **Insight:** two or three clues the player links on the board. A valid link produces a new card at once.
- **Question:** each act poses two or three. Each takes one answer.
- **Confrontation:** naming an answer to someone with power. Only then does the game respond to whether it was right.

Rules:

1. Every insight needed for the truth can be reached at least three ways: looking, talking, or getting into somewhere Ruth should not be.
2. Linking is free and immediate. Answers are never checked until they are spoken to someone.
3. Each act has a planted answer with real support, and one *seen* clue that breaks it.
4. A wrong answer moves the story on along a worse road. There is no fail screen for being wrong.
5. Time removes evidence: scenes are cleaned, people leave on the morning bus, documents are shredded. The last route to a required insight is never removed.

**Objects.** There are no quest items. Every object has what it does, what it shows, and who reacts to it. Daniel's pistol is a weapon, the murder weapon, and the thing Sam cannot look at. Tray 114 is thirty kilograms of rock, the proof, and something each of the four will kill for a different reason.

## 9. Conversations

Named characters are spoken to face to face: the view cuts from the isometric town to a first-person frame, a large portrait against the room Ruth is standing in, in the manner of Dune (1992). A short list of commands sits at the bottom right.

- **Ask about…** topics, which exist because of clues and leads Ruth holds.
- **Show…** any object or clue card.
- **Press** when what they said conflicts with something *seen*.
- **Tell…** give them a clue or an answer. Ruth chooses which, and can leave part of it out. She cannot invent facts, but she can tell different people different true things.
- **Offer…** money, silence, a lift out of town.
- **Leave.**

**Night resolves the town.** Each of the four acts overnight on what they currently believe: who has the box, who killed Daniel, who is talking to Ruth. The morning shows the result, such as a burned scrap yard, a missing guard, a new man on the gate, or a body. This is the engine of acts II and III, and the reason the body count is the player's.

## 10. Stealth

Used for places Ruth has no right to be (the station at night, the assay lab, Steyn's compound, the big house, No. 2 shaft) and for all of Thursday.

- Guards see in cones, hear footsteps on gravel and iron sheeting, and follow routes tied to the shift clock.
- Darkness hides; a torch reveals both ways.
- Distractions are ordinary: a thrown stone, a car alarm, a phone left ringing.
- Ruth can hold someone from behind and put them down without killing them. They wake later and remember her.
- Being seen is not instant failure. People ask what she is doing, and in daylight a good answer often works.
- A drawn weapon changes every conversation in town.

## 11. Weapons, armour, health

No upgrades, no attachments, no ammunition types, no experience.

- **Weapons:** Ruth's 9 mm pistol with two magazines. Found during play: a revolver, a pump shotgun, a hunting rifle. Improvised: a torch, a tyre iron. She carries one long gun and one handgun. Ammunition is counted in single rounds and there is little of it.
- **Armour:** one covert vest, obtainable on Wednesday. It stops pistol rounds a few times, slows her slightly, and is noticed in conversation.
- **Health:** three states: fine, hurt, badly hurt. A first-aid kit takes her up one state. The clinic treats her fully but costs a block and the nurse asks questions. Wounds carry over to the next day.
- **Enemies** are as fragile as she is. Two or three hits ends anyone. A fight with more than four opponents is one to avoid or to arrange allies for.
- Death reloads the start of the current block.

## 12. Camera and controls

- 3D town, fixed-pitch isometric camera, free rotation, three zoom steps. Walls and roofs between camera and Ruth fade.
- WASD or click to move, mouse to aim, one interact key with a small radial (Look, Take, Use, Talk).
- **Focus** (hold): the view desaturates and anything examinable nearby is outlined.
- Crouch, lean at corners, and a noise ring under Ruth's feet when moving.

## 13. First build

Monday only: arrival, the crowd at the station, three conversations, the room and the board, the night entry into the station, the drive to the big house. About forty minutes. It exercises movement, Focus, conversation, clue capture, linking, stealth and one possible gunfight.

Engine is still open. With co-op gone, a browser build (Three.js) remains the fastest route to something playable and testable here; Unity or Godot are equally reasonable for the full game.

## 14. Open decisions

- Confirm the three assumptions at the top.
- Whether Ruth is the right protagonist: an outsider, as in the source, or someone who grew up in Doornrand.
- Art direction, and how to draw this town without postcard detail. The earlier concept art no longer applies.
- Title.
