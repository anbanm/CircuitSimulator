# AFTERIMAGE (working title) — Game Design

A cyberpunk detective-noir action RPG. Isometric, rotatable camera. Solo or 2–4 player online co-op.

Status: design draft, no code yet. Names, plot and numbers are proposals to be changed.

---

## 1. The game in one paragraph

You are a licensed finder (private investigator) in Saltgrave, a half-drowned coastal city run by corporations and policed by people on their payroll. You live there: you have an office with a bed and a wall you pin evidence to, rent due on Fridays, a noodle stall that knows your order. Cases come in. You walk the districts, search scenes, talk to people face to face, fight when talking fails, and assemble what you learned into a theory. The game never tells you whether the theory is right until you act on it.

## 2. Design pillars

1. **One world, one item system.** There are no "quest items". Every story object is a normal game object with a use, and every useful object can be evidence. (Section 5.)
2. **The player solves the case, not the character.** The game hands out facts. Conclusions are the player's, and wrong ones are playable.
3. **Violence is a choice with investigative cost.** Combat is fast and can get heavy, but dead people do not testify and shot-up rooms lose evidence.
4. **A city you live in.** Time passes, people keep schedules, places remember what you did there.
5. **Simple rules, few numbers.** Four attributes, small inventory, no loot showers.
6. **Co-op is the same game.** No separate mode, no separate content. Partners split up, share one case board, and argue about the theory.

## 3. Camera, controls, feel

- 3D world, fixed-pitch isometric camera (about 50°), free 360° rotation with Q/E or right stick, three zoom steps. Walls and roofs between camera and players fade out. Interiors are entered without a loading screen where possible.
- Movement: WASD or click-to-move (both supported). Gamepad twin-stick.
- One interact key. Hold it to open a radial with the verbs available on that object (Look, Take, Use, Hack, Talk).
- **Focus mode** (hold Tab): time slows slightly in solo, the world desaturates, and everything examinable within range gets an outline. Range and detail scale with the Read attribute. This is the investigation equivalent of holding Alt in Diablo to see loot.

## 4. Simplified RPG rules

**Attributes** (1–5 each, start with 7 points):

| Attribute | Combat | Investigation |
|---|---|---|
| Grit | Health, melee damage, knockdown resistance | Force doors, intimidate, endure hazards |
| Reflex | Gun handling, dodge recharge | Sleight of hand, tailing, chases |
| Wire | Implant charge, hacking in combat | Hack terminals, cameras, locks, read implants |
| Read | Crit on staggered enemies | Focus mode detail, spot lies, extra dialogue topics |

No attribute gates a case solution. A low attribute means a longer or louder route, never a dead end.

**Backgrounds** (pick one, sets starting attributes, one talent, and how some people greet you): Ex-Harbor Police, Fixer, Street Medic, Runner.

**Progression.** Level 1 to 10 across the whole game. XP comes from closing leads and cases, not from kills. Each level gives one attribute point or one talent. Talents are flat unlocks ("Knockouts are silent", "Read one extra afterimage frame"), with no trees.

**Loadout.** Two weapons, one coat (armour and pockets), three implant slots, a belt of four consumables. Around 40 hand-placed items in the whole game rather than random drops.

**Combat.** Real-time. Aim with mouse or right stick, dodge roll on a short recharge, two weapon attacks, up to three implant abilities on cooldown using Charge. Enemies have Health and Stagger; a staggered enemy can be **subdued** (non-lethal takedown) instead of killed. Subdued enemies can be questioned afterwards and are still around later in the story. Solo players get an optional AI partner who is also a story character.

**Resources.** Credits (rent, bribes, gear, food), Charge (implant energy), and Heat per district (how much the local power wants you gone; raises patrols and closes doors).

## 5. Story items are game items

Every object carries up to three facets. The same object is a different thing depending on what you do with it.

- **Use**: what it does mechanically (weapon, key, implant, consumable).
- **Evidence**: what facts it holds, revealed by examining it, hacking it, or showing it to an expert.
- **Leverage**: who reacts when you show it to them, and how.

Examples:

| Object | Use | Evidence | Leverage |
|---|---|---|---|
| Victim's optic implant | Equippable eye, grants low-light vision | Holds his last recorded frames (an *afterimage*) | His sister breaks down; the surgeon who cut it out panics |
| Harbor Police sidearm | A good pistol | Ballistics match a body in Case 3 | Carrying it openly raises Heat with police, opens doors with the dock union |
| Bathhouse wristband | Opens lockers at the Glasshouse | Timestamp places the victim there at 01:40 | The attendant stops pretending not to know him |

Consequences follow from this: selling, losing, or destroying an item removes its evidence. Equipping the dead man's eye is useful and is also something people notice. Looting is investigating.

## 6. The case system

This replaces the usual quest log. There are no objective markers and no "go here" text.

### 6.1 Building blocks

- **Clue**: one atomic fact with a source and a status. *Observed* (you saw it), *Claimed* (someone said it), or *Recorded* (from a device, which can be forged). Example: "Fresh water in the lungs. (Observed, coroner's slab)".
- **Lead**: an open question that a clue raises. Created automatically. "Where does a dock auditor meet chlorinated water?" Leads are the to-do list. Each names people and places it touches, and those get a soft highlight on the map. Closing a lead grants XP.
- **Insight**: a conclusion made by the player linking two or three clues on the board. Insights unlock dialogue topics, new locations, and contradictions. Example: wristband timestamp + coroner's time of death gives "He was at the Glasshouse when he died."
- **Question**: each case has three to five. Who, where, how, why. Each Question has a slot on the board that accepts one Answer backed by the player's Insights and clues.
- **Theory**: the current set of Answers. It can be incomplete or wrong.
- **Confrontation**: the point where the player acts on the Theory. Only then does the world respond to whether it was right.

### 6.2 The loop

```
Scene / person / fight  ->  Clues  ->  Leads (where next)
                              |
                         Case board: link clues -> Insights
                              |
                     Fill Questions -> Theory
                              |
                       Confrontation -> Revelation -> consequences
```

### 6.3 Rules that keep it fair

1. **Three routes.** Every Insight needed for the true answer is reachable from at least three clues obtained by different means: looking, talking, hacking, fighting. An action-heavy player who kicks in the door still finds the comm log on the man they subdued.
2. **Linking is cheap, answering is not.** A valid link produces an Insight immediately; an invalid one says "nothing connects these". Answers to Questions are never validated until the Confrontation. This rewards attention without letting anyone brute-force the ending.
3. **False trails are authored.** Each case contains a planted wrong answer with real supporting clues, and at least one clue that breaks it. The break is always an *Observed* clue.
4. **No fail state.** A wrong Theory moves the story forward along a worse branch: the wrong person takes the fall, an ally is lost, the real culprit gets a head start. Later cases remember.
5. **Evidence decays.** Rain washes outdoor scenes after a time block. Witnesses move on. Firefights destroy examinables in the room. Decay never removes the last route to a required Insight.
6. **Contradictions.** In conversation, presenting a clue that conflicts with a *Claimed* clue from that speaker opens a Press. Success converts the claim, usually into something better.

### 6.4 The case board

A physical wall in your office and a full-screen UI anywhere else. Columns per Question, clue cards with source and status icons, string between linked cards. The Read attribute adds faint hints on cards that have an unused link. In co-op it is one shared board (Section 9).

### 6.5 Data shape (engine-neutral)

```json
{
  "clue": { "id": "c1_lungs_fresh", "text": "Fresh water in the lungs.",
            "status": "observed", "source": "coroner_slab",
            "tags": ["tobias", "cause"], "raises": ["lead_where_drowned"] },
  "insight": { "id": "i1_died_glasshouse", "from": ["c1_lungs_fresh", "c1_wristband_time"],
               "text": "He drowned at the Glasshouse.", "unlocks": ["topic:attendant_night_shift"] },
  "question": { "id": "q1_where", "answers": ["canal", "glasshouse", "apartment"],
                "truth": "glasshouse", "supports": { "glasshouse": ["i1_died_glasshouse"] } }
}
```

All story content is data like this. Dialogue, items, and scenes reference clue ids; nothing in the engine knows the plot.

## 7. Face-to-face scenes

Modelled on the conversation screens of Dune (1992): you look straight at the person.

- Trigger: Talk to a named character, or a scripted beat. The isometric view pushes in on the character and cuts to a **first-person frame**: a large animated portrait over a painted backdrop of the place you are standing in, at the current time of day and weather.
- The bottom of the screen is a short command list, not a dialogue tree of full sentences:
  - **Ask about…** topic chips. Topics exist because of clues, leads, and insights you hold. Know more, ask more.
  - **Show…** any item or clue card. This is where Leverage and Contradictions fire.
  - **Press** when a contradiction is open. Uses Grit (threat) or Read (precision); the choice colours the relationship.
  - **Offer…** credits, a favour, silence.
  - **Come with me** / **Wait here** for characters who can accompany you.
  - **Leave.**
- Lines are short and voiced or text-ticked. The portrait reacts: eyes away on a lie if your Read is high enough.
- Statements that matter drop a clue card onto the screen edge as they are spoken, so the player sees the system capture it.
- **Travel interludes.** Riding the maglev between districts plays a window-view sequence with the detective's voice-over summarising open leads. It is the recap screen, in character.
- **Revelation scenes** use the same format with one addition: the player lays their Answers on the table one at a time and the other person responds to each.

## 8. The city

**Saltgrave.** Sea walls failed a generation ago. The rich moved up; everyone else learned to live at the waterline.

| District | Character | Power | Opens |
|---|---|---|---|
| The Shallows | Flooded low town, stilt housing, canals, your office | Koi Dogs (gang) | Start |
| Lantern Row | Night markets, memory parlours, clinics | Parlour owners | Case 2 |
| Brasswork | Docks, container stacks, union halls | Harbor Police, dock union | Case 3 |
| The Underline | Dead metro beneath it all, squatters, black clinics | Nobody | Case 3 |
| The Spire | Corporate arcology, clean and watched | Halden-Voss | Case 4 |

**Living in it.**

- **Time.** Four blocks per day: morning, afternoon, night, small hours. Time advances with travel, sleep, and some actions. Shops, shifts, and crowds change by block.
- **Schedules.** Named characters have places to be. The coroner drinks at the same bar after shift, and is more talkative there.
- **Your office.** Bed (advance time, heal), the case board, a stash, a workbench for examining items in detail, the door clients knock on. It is also the co-op lobby. It changes as cases close: a commendation on the wall, or a brick through the window.
- **Upkeep.** Rent weekly. Food stalls give small timed buffs and are where street rumours surface as clues. Missing rent does not end the game; it changes who you owe.
- **Street work.** Small cases from the neighbourhood (missing courier, stolen prosthetic, cheating partner) built from the same clue system with two Questions each. They pay rent and build reputation.
- **Reputation and Heat** per district decide prices, which doors open, and who starts shooting on sight.
- **Persistence.** People you subdued remember. People you killed are missing from later scenes, and others notice.

## 9. Co-op (online, 2–4)

- **Hosting.** One player's save is the canon world. Others join by code, drop in and out. Guests keep their character's XP, gear, and credits; case outcomes belong to the host's world, and guests get a "consulted on" record so they can replay the case as host with knowledge of it flagged.
- **Splitting up is the point.** Players can be anywhere in the same district at once. One questions the attendant while another searches the lockers. Combat encounters scale to the players actually present.
- **Shared board, personal knowledge.** Every clue goes to the shared board, tagged with who found it. Only the finder sees the full detail until they pin it or tell the others. Players talk; that is the co-op mechanic.
- **Face-to-face scenes.** The player who starts one is the lead. Players nearby join as seconds: they see the scene, can queue a topic or an item for the lead to use, and can Press with their own attributes. Players elsewhere keep playing and get a transcript in the case file.
- **Committing.** Answers on the board can be placed by anyone. Starting a Confrontation needs a majority. Dissent is recorded and some characters comment on it.
- **Downed and revive** in combat. A full wipe wakes everyone in a clinic with a bill and a lost time block.
- **Network model.** Server-authoritative simulation (host machine or small dedicated relay). Two channels: high-rate world state (movement, combat), and a low-rate ordered event log for case state (clues, board, dialogue), which also serves as the save format.

## 10. Plot (draft)

Five cases, one conspiracy. Each case stands alone and leaves one thread hanging.

**Fixed truth, hidden from the player:** Halden-Voss sells *Continuity*: a grown body and a copied mind for clients who can afford not to die. Copies are legally company property. Copies also drift without regular resync from the original, so the originals are kept alive in storage beneath the Spire. Bodies come in through Brasswork as medical freight, cleared by Harbor Police.

1. **The Drowned Client.** Ines Marlowe hires you to find her brother Tobias, a dock auditor. You find him dead in a Shallows canal with his optic implant cut out, and a frame pointing at the Koi Dogs. (Vertical slice, Section 11.)
2. **Paper Lanterns.** The eye went through Lantern Row's memory parlours. Someone is buying every copy of its last recording. You learn what Tobias was auditing: freight listed as "blanks".
3. **Brasswork.** The docks. A union boss who wants the blanks gone, a police lieutenant who wants you gone, and a warehouse of bodies with no one in them. The heaviest action in the game.
4. **The Spire.** Inside Halden-Voss. The Ines Marlowe in their records died two years ago. Your client is a copy. Tobias found out and was killed for it, and she hired you because the gaps in her own memory frightened her.
5. **Low Tide.** The originals are alive under the Spire. Waking them ends the copies, your client included. Leaving them keeps the machine running. Selling what you know buys you out of Saltgrave. The ending is the last Confrontation, built from every Theory you committed to along the way.

Recurring cast: Ines Marlowe (client), Lt. Aurel Dace (Harbor Police, your ex-partner if you chose that background), Mama Carp (Koi Dogs), Dr. Sable (surgeon who works for anyone), Okonkwo (coroner, your one honest source), Wen (noodle stall, hears everything), and your partner Juno Vale (the AI companion in solo).

## 11. Vertical slice: "The Drowned Client"

The first thing to build. One district (The Shallows), about 60–90 minutes, exercises every system.

**Truth.** Tobias was drowned in a private pool at the Glasshouse bathhouse by Sgt. Rook of Harbor Police. Dr. Sable removed the eye on the spot. The body was dumped in the canal and tagged with a Koi Dogs mark to look like a gang killing.

**Questions.**

| # | Question | Planted answer | True answer |
|---|---|---|---|
| Q1 | Where did he die? | The canal | The Glasshouse |
| Q2 | Who took his eye? | Koi Dogs | A surgeon (Dr. Sable) |
| Q3 | Who wants the Dogs blamed? | Nobody, they did it | Harbor Police (Rook) |

**Clues and routes.**

| Clue | Where | How | Feeds |
|---|---|---|---|
| Fresh, chlorinated water in lungs | Coroner's slab | Look / ask Okonkwo | Q1 |
| Glasshouse wristband, stamped 01:40 | Body's coat lining | Look (Focus) | Q1 |
| Attendant saw him go to the private pools | Glasshouse | Talk, or Show wristband | Q1 |
| Pool drain log, emergency flush 02:05 | Glasshouse back office | Hack | Q1 |
| Eye socket cut is surgical, cauterised | Coroner's slab | Look, better with Wire | Q2 |
| Koi Dogs enforcer: "We don't do eyes, no market" | Dogs' den or street | Talk, or subdue and question | Q2, Q3 |
| Sable's clinic invoice, cash, that night | Lantern-side clinic | Hack, steal, or Press Sable | Q2 |
| Gang tag at the scene uses last year's mark | Canal wall | Look; a Dog confirms | Q3 |
| Patrol log gap, Rook's unit, 01:30–02:30 | Precinct terminal / Dace | Hack, or ask as ex-police | Q3 |
| Rook's comm: "Tell the doc it's done" | On Rook or his men | Subdue and loot | Q2, Q3 |

**Confrontation.** At the Koi Dogs' den, at night.

- If the Theory blames the Dogs: you go in shooting. A large fight. You win, the case is "closed", Harbor Police thank you, and Mama Carp is dead or an enemy for the rest of the game.
- If the Theory clears them: you go in to talk. Halfway through the face-to-face with Mama Carp, Rook's unit raids the den to tidy up. Same scale of fight, on the other side, with the Dogs beside you. Rook can be subdued.

**Revelation.** Face-to-face with Ines in your office. You lay out your Answers. You choose what to hand over: the truth, the convenient version, or the eye itself. If you recovered the eye and play its afterimage, the last clear frame before the water is Ines's face. That is the thread into Case 2.

**Slice content budget.** One district block (canal, office, coroner, Glasshouse, clinic, den, precinct lobby), 8 speaking characters with portraits, 3 enemy types, 6 weapons and implants, about 25 clues, one travel interlude, one street case.

## 12. Technology

Engine is undecided. The design above does not depend on it; content is data (Section 6.5).

| Option | For | Against |
|---|---|---|
| **Browser: Three.js + TypeScript, Node relay (Colyseus or similar)** | Join co-op by link, nothing to install. Can be built and playtested end to end in the cloud sessions used so far. | Lower visual ceiling, more hand-built tooling |
| **Unity + Netcode for GameObjects** | You already have Unity experience in this repository. Strong tooling for lighting, animation, navmesh. | Cannot be compiled or run in these sessions; first test of every change happens on your machine |
| **Godot 4** | Free, light, good 3D isometric support, headless export can run here | Smaller ecosystem for co-op and animation |

Recommendation: browser for the vertical slice, because it gets a playable co-op build in front of you fastest and every change can be verified before you see it. The case data, dialogue, and plot carry over unchanged if the full game later moves to Unity.

## 13. Build order for the slice

1. Walkable Shallows block, rotatable camera, wall fade, interact radial, Focus mode.
2. Item facets and inventory; clue capture; case board with linking.
3. Face-to-face scene player driven by data; Ask, Show, Press.
4. Combat: one gun, one melee, dodge, stagger and subdue, three enemy types.
5. Case 1 content pass, Confrontation branches, Revelation scene.
6. Co-op: session join, synced movement and combat, shared board, seconds in scenes.
7. Office, time blocks, rain decay, one street case.

## 14. Open decisions

- Engine (Section 12).
- Art direction for portraits: painted stills with a few animated layers, or 3D heads rendered close.
- Voice: full, partial (key scenes), or text only.
- How dark the endings go, and whether any ending is unambiguously good.
- Title.
