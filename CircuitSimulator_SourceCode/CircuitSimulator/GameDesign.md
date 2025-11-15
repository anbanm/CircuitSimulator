# Circuit Simulator 3D: Game Design Document
## An Inquiry-Based Learning Tool for Grade 7 Simple Circuits

### Version 1.0 | December 2024

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Educational Framework](#educational-framework)
3. [Learning Objectives](#learning-objectives)
4. [Game Design Philosophy](#game-design-philosophy)
5. [Core Gameplay Mechanics](#core-gameplay-mechanics)
6. [Inquiry-Based Learning Features](#inquiry-based-learning-features)
7. [Misconception Detection & Remediation](#misconception-detection--remediation)
8. [Assessment & Progress Tracking](#assessment--progress-tracking)
9. [Technical Implementation](#technical-implementation)
10. [User Experience Design](#user-experience-design)
11. [Academic Foundation & References](#academic-foundation--references)

---

## Executive Summary

Circuit Simulator 3D is an educational game designed to teach simple electrical circuits to Grade 7 learners (ages 12-13) through inquiry-based learning. Unlike traditional circuit simulators that focus on technical accuracy, this tool prioritizes pedagogical effectiveness by actively detecting and addressing common misconceptions while encouraging exploration and discovery.

### Key Innovation Points
- **Real-time misconception detection** based on circuit construction patterns
- **Scaffolded inquiry experiences** that guide without constraining exploration
- **Visual-first feedback** showing current flow, voltage drops, and power consumption
- **Gamified challenge system** aligned with curriculum standards

---

## Educational Framework

### Theoretical Foundation

Our approach is grounded in **Constructivist Learning Theory** (Piaget, 1952; Vygotsky, 1978) and **Inquiry-Based Science Education** (IBSE) principles (Pedaste et al., 2015). The game implements the 5E Instructional Model (Bybee et al., 2006):

1. **Engage** - Capture attention with interactive 3D circuits
2. **Explore** - Free-play sandbox mode for discovery
3. **Explain** - Just-in-time explanations when misconceptions detected
4. **Elaborate** - Challenge scenarios applying concepts
5. **Evaluate** - Embedded assessment through gameplay

### Cognitive Load Management

Following Sweller's Cognitive Load Theory (1988), we manage cognitive load through:
- **Intrinsic Load Reduction**: Progressive complexity introduction
- **Extraneous Load Minimization**: Clean, focused interface
- **Germane Load Optimization**: Scaffolded problem-solving

---

## Learning Objectives

### Primary Learning Outcomes (Grade 7 Science Curriculum)

#### Knowledge & Understanding
- Define current, voltage, and resistance
- Identify circuit components and their symbols
- Distinguish between series and parallel circuits
- State Ohm's Law (V = I × R)

#### Skills & Processes
- Construct complete circuits using appropriate components
- Measure and calculate current, voltage, and resistance
- Predict circuit behavior before testing
- Troubleshoot non-functioning circuits

#### Scientific Thinking
- Form hypotheses about circuit behavior
- Design experiments to test predictions
- Analyze patterns in electrical measurements
- Draw conclusions from evidence

### Alignment with Educational Standards

| Standard | Organization | Grade Level | Topics Covered |
|----------|-------------|------------|----------------|
| PS2.B | NGSS (USA) | 6-8 | Electric and Magnetic Forces |
| SCN 3-09a | CfE (Scotland) | Second Level | Electrical Circuits |
| ACSSU097 | Australian Curriculum | Year 6 | Electrical Circuits |
| KS3 Physics | UK National Curriculum | Year 7-9 | Current Electricity |

---

## Game Design Philosophy

### Core Principles

#### 1. **Exploration Over Instruction**
Students learn by doing, not by reading instructions. The game provides a sandbox environment where learners can:
- Place components freely in 3D space
- Connect wires between any terminals
- See immediate visual feedback
- Make and learn from mistakes safely

#### 2. **Visible Thinking**
Abstract electrical concepts become concrete through visualization:
- **Animated current flow** shows electron movement direction
- **Color-coded voltage** displays potential differences
- **Brightness variations** in bulbs demonstrate power consumption
- **Spark effects** indicate short circuits or overloads

#### 3. **Productive Failure**
Mistakes are learning opportunities (Kapur, 2008):
- Non-functioning circuits prompt investigation
- Misconception alerts guide reflection
- "Why didn't this work?" prompts encourage hypothesis formation
- Multiple solution paths are celebrated

---

## Core Gameplay Mechanics

### Component Manipulation

#### Available Components
1. **Battery** (6V, 12V options)
   - Visual: Rectangular with clear + and - terminals
   - Function: Provides voltage source
   - Learning: Direction of current flow

2. **Resistor** (10Ω, 20Ω, 50Ω, 100Ω)
   - Visual: Cylindrical with color bands
   - Function: Limits current flow
   - Learning: Ohm's Law relationships

3. **Light Bulb** (5Ω internal resistance)
   - Visual: Sphere that glows when powered
   - Function: Visual indicator of current
   - Learning: Power consumption, brightness variation

4. **Switch** (Open/Closed states)
   - Visual: Toggle mechanism
   - Function: Control circuit completion
   - Learning: Complete vs incomplete circuits

5. **Connecting Wire**
   - Visual: Flexible cable with connection points
   - Function: Carries current between components
   - Learning: Conductive paths

### Interaction Modes

#### Select Mode (V key)
- Click to select components
- Drag to move in 3D space
- Right-click for properties
- Delete with X key

#### Connect Mode (C key)
- Click component terminals to start wire
- Preview wire follows cursor
- Click second terminal to complete
- ESC to cancel connection

#### Component Mode
- Quick placement with keyboard shortcuts
- B = Battery, R = Resistor, L = Light bulb, S = Switch
- Components appear in camera view
- Automatic grid snapping

### Real-Time Feedback Systems

#### Visual Indicators
- **Current Flow Animation**: Moving dots along wires
- **Voltage Labels**: Numerical displays on components
- **Power Indicators**: Glow intensity proportional to power
- **Connection Points**: Colored terminals (red = positive, black = negative)

#### Audio Feedback (Future Implementation)
- Connection sounds when wires attach
- Buzzing for short circuits
- Click sounds for switch toggles
- Success chimes for completed challenges

---

## Inquiry-Based Learning Features

### Guided Inquiry Scaffolding

#### Level 1: Structured Inquiry
Students follow step-by-step instructions with clear objectives:
```
Challenge: Light the Bulb
1. Place a battery in the workspace
2. Add a light bulb nearby
3. Connect the positive terminal to one bulb terminal
4. Connect the negative terminal to the other bulb terminal
5. Observe: What happens to the bulb?
```

#### Level 2: Guided Inquiry
Students receive goals but determine their own methods:
```
Challenge: Brightness Control
Goal: Make two bulbs glow with different brightness
Hint: Think about how resistance affects current...
```

#### Level 3: Open Inquiry
Students formulate their own questions and investigations:
```
Sandbox Mode: Unlimited Components
Investigate: How does the number of batteries affect bulb brightness?
Create your own experiment and draw conclusions.
```

### Prediction-Observation-Explanation (POE) Cycle

Before circuit completion, students:
1. **Predict** - "What will happen when I close this switch?"
2. **Observe** - Watch actual behavior
3. **Explain** - Reconcile prediction with observation

The game tracks prediction accuracy and provides targeted support when patterns of misconceptions emerge.

### Virtual Laboratory Notebooks

Students can:
- Save circuit designs with annotations
- Record measurements and observations
- Compare multiple circuit configurations
- Export findings for classroom discussion

---

## Misconception Detection & Remediation

### Common Misconceptions Addressed

Based on research by Shipstone (1984) and McDermott & Shaffer (1992):

#### M1: The Sink Model
**Misconception**: "Current is 'used up' by components"
**Detection**: Student creates circuit expecting dimmer bulbs further from battery
**Remediation**:
- Show current measurement at multiple points
- Highlight that current is same throughout series circuit
- Animation showing electrons returning to battery

#### M2: Sequential Model
**Misconception**: "Current flows from positive to negative and stops"
**Detection**: Student only connects positive terminal
**Remediation**:
- Alert: "Circuits need a complete path!"
- Ghost wire showing required return path
- Analogy to water pump needing inlet and outlet

#### M3: Constant Current Source
**Misconception**: "Batteries provide constant current regardless of circuit"
**Detection**: Student expects same brightness with different resistances
**Remediation**:
- Show how current changes with resistance
- Interactive Ohm's Law calculator
- Side-by-side comparison of different circuits

#### M4: Power Supply Confusion
**Misconception**: "Batteries supply energy, not voltage"
**Detection**: Confusion when parallel branches have different currents
**Remediation**:
- Visualize voltage as "electrical pressure"
- Show how voltage divides in series
- Demonstrate voltage same across parallel branches

### Adaptive Feedback System

The game maintains a **Misconception Profile** for each student:
```javascript
MisconceptionProfile {
  sinkModelCount: 0,
  sequentialModelCount: 0,
  currentSourceCount: 0,
  lastDetectedType: null,
  remediationLevel: 1
}
```

Remediation becomes more explicit with repeated misconceptions:
- **Level 1**: Subtle visual hints
- **Level 2**: Pop-up suggestions
- **Level 3**: Guided tutorial intervention
- **Level 4**: Teacher notification for direct instruction

---

## Assessment & Progress Tracking

### Formative Assessment

#### Real-Time Performance Metrics
- Circuit completion time
- Number of attempts before success
- Prediction accuracy rate
- Misconception frequency
- Help system usage

#### Competency Indicators
```
Basic Understanding:
☑ Completes simple series circuit
☑ Identifies positive/negative terminals
☑ Uses switch to control circuit

Developing Proficiency:
☑ Builds parallel circuit correctly
☑ Predicts relative bulb brightness
☑ Calculates basic current values

Advanced Application:
☑ Designs mixed series-parallel circuits
☑ Optimizes for specific resistance
☑ Explains current distribution patterns
```

### Summative Assessment

#### Challenge Mode Scoring
- **Efficiency Points**: Fewer components = higher score
- **Accuracy Points**: Correct predictions boost score
- **Speed Points**: Faster completion adds bonus
- **Innovation Points**: Novel solutions rewarded

#### Portfolio Generation
Students compile a digital portfolio containing:
- Completed challenge solutions
- Circuit design explanations
- Measurement data tables
- Reflection journal entries

---

## Technical Implementation

### Architecture Overview

```
Unity 6 Engine
├── Service-Oriented Architecture
│   ├── ServiceLocator Pattern
│   ├── Dependency Injection
│   └── Event-Driven Communication
├── Component System
│   ├── CircuitComponent3D (Unity representation)
│   ├── CircuitCore (Electrical logic)
│   └── CircuitSolver (Nodal analysis)
├── Educational Layer
│   ├── MisconceptionDetector
│   ├── ChallengeManager
│   └── ProgressTracker
└── Rendering Pipeline
    ├── Universal Render Pipeline (URP)
    ├── Real-time lighting
    └── Post-processing effects
```

### Circuit Solving Algorithm

Implements modified nodal analysis with educational considerations:

```csharp
// Simplified for Grade 7 understanding
foreach (Node in Circuit) {
    // Apply Kirchhoff's Current Law
    sumCurrentIn = sumCurrentOut

    // Apply Ohm's Law
    V = I × R

    // Check for educational moments
    if (DetectMisconception()) {
        TriggerRemediation()
    }
}
```

### Performance Optimizations

- **Object Pooling**: Reuse wire and component instances
- **LOD System**: Reduce detail for distant objects
- **Culling**: Hide off-screen components
- **Batch Rendering**: Combine similar meshes

---

## User Experience Design

### Interface Layout

```
┌─────────────────────────────────────────────┐
│  [Mode] [Components] [Actions]    [Values]  │
├─────────┬───────────────────────┬──────────┤
│         │                       │          │
│ Palette │   3D Workspace        │  Info    │
│         │                       │  Panel   │
│ [B] [R] │   (Circuit Building)  │          │
│ [L] [S] │                       │ V: 12V   │
│ [J]     │                       │ I: 0.5A  │
│         │                       │ R: 24Ω   │
└─────────┴───────────────────────┴──────────┘
```

### Accessibility Features

- **Colorblind Modes**: Alternative color schemes
- **Text Scaling**: Adjustable label sizes
- **Keyboard Navigation**: Full keyboard control
- **Screen Reader Support**: Component descriptions
- **Simplified Mode**: Reduced visual complexity option

### Progressive Disclosure

Information revealed as needed:
1. **Beginner**: Only voltage shown
2. **Intermediate**: Add current display
3. **Advanced**: Include power calculations
4. **Expert**: Show internal resistance

---

## Academic Foundation & References

### Foundational Research

**Constructivist Learning Theory**
- Piaget, J. (1952). *The Origins of Intelligence in Children*. International Universities Press.
- Vygotsky, L. S. (1978). *Mind in Society: The Development of Higher Psychological Processes*. Harvard University Press.

**Inquiry-Based Science Education**
- Pedaste, M., et al. (2015). "Phases of inquiry-based learning: Definitions and the inquiry cycle." *Educational Research Review*, 14, 47-61.
- Bybee, R. W., et al. (2006). *The BSCS 5E Instructional Model: Origins and Effectiveness*. BSCS.

**Cognitive Load Theory**
- Sweller, J. (1988). "Cognitive load during problem solving: Effects on learning." *Cognitive Science*, 12(2), 257-285.
- Mayer, R. E. (2009). *Multimedia Learning* (2nd ed.). Cambridge University Press.

### Circuit Education Research

**Misconception Studies**
- Shipstone, D. M. (1984). "A study of children's understanding of electricity in simple DC circuits." *European Journal of Science Education*, 6(2), 185-198.
- McDermott, L. C., & Shaffer, P. S. (1992). "Research as a guide for curriculum development: An example from introductory electricity." *American Journal of Physics*, 60(11), 994-1003.
- Engelhardt, P. V., & Beichner, R. J. (2004). "Students' understanding of direct current resistive electrical circuits." *American Journal of Physics*, 72(1), 98-115.

**Educational Game Design**
- Squire, K. (2011). *Video Games and Learning: Teaching and Participatory Culture in the Digital Age*. Teachers College Press.
- Gee, J. P. (2007). *What Video Games Have to Teach Us About Learning and Literacy*. Palgrave Macmillan.
- Clark, D. B., Tanner-Smith, E. E., & Killingsworth, S. S. (2016). "Digital games, design, and learning: A systematic review and meta-analysis." *Review of Educational Research*, 86(1), 79-122.

### Similar Educational Tools

**PhET Interactive Simulations**
- Wieman, C. E., Adams, W. K., & Perkins, K. K. (2008). "PhET: Simulations that enhance learning." *Science*, 322(5902), 682-683.
- Reference: https://phet.colorado.edu/en/simulation/circuit-construction-kit-dc

**TEAL (Technology Enabled Active Learning)**
- Dori, Y. J., & Belcher, J. (2005). "How does technology-enabled active learning affect undergraduate students' understanding of electromagnetism concepts?" *The Journal of the Learning Sciences*, 14(2), 243-279.

**Algodoo Physics Sandbox**
- Euler, M., & Müller, A. (2011). "Physics learning with Algodoo: Students' ideas about light propagation." *Physics Education*, 46(4), 417.

### Assessment in Educational Games

- Shute, V. J. (2011). "Stealth assessment in computer-based games to support learning." *Computer Games and Instruction*, 55(2), 503-524.
- Mislevy, R. J., et al. (2014). "Psychometric considerations in game-based assessment." *GlassLab Research*.

### Productive Failure Research

- Kapur, M. (2008). "Productive failure." *Cognition and Instruction*, 26(3), 379-424.
- Kapur, M., & Bielaczyc, K. (2012). "Designing for productive failure." *Journal of the Learning Sciences*, 21(1), 45-83.

---

## Appendices

### A. Curriculum Alignment Matrix

| Topic | NGSS | UK KS3 | Australian | Ontario |
|-------|------|--------|------------|---------|
| Complete Circuits | MS-PS2-3 | 3.2.1 | ACSSU097 | SNC1D |
| Series/Parallel | MS-PS2-5 | 3.2.2 | ACSSU097 | SNC1D |
| Current Flow | MS-PS3-2 | 3.2.3 | ACSSU219 | SNC1P |
| Resistance | MS-PS3-2 | 3.2.4 | ACSSU219 | SNC1D |
| Voltage | MS-PS2-3 | 3.2.5 | ACSSU219 | SNC1D |

### B. Implementation Timeline

**Phase 1: Core Mechanics (Months 1-3)**
- Basic component placement
- Wire connections
- Circuit solving

**Phase 2: Educational Features (Months 4-6)**
- Misconception detection
- Challenge scenarios
- Progress tracking

**Phase 3: Polish & Assessment (Months 7-9)**
- User testing with Grade 7 students
- Teacher feedback integration
- Performance optimization

**Phase 4: Deployment (Months 10-12)**
- School pilot programs
- Teacher training materials
- Assessment integration

### C. Evaluation Metrics

**Learning Effectiveness**
- Pre/post concept inventory scores
- Misconception reduction rates
- Transfer task performance
- Long-term retention testing

**Engagement Metrics**
- Time on task
- Voluntary usage
- Challenge completion rates
- Peer collaboration frequency

**Teacher Adoption**
- Ease of integration
- Curriculum alignment
- Assessment utility
- Professional development needs

---

## Conclusion

Circuit Simulator 3D represents a synthesis of educational research, game design principles, and technical innovation. By focusing on inquiry-based learning and misconception remediation, it addresses the specific challenges Grade 7 learners face when understanding electrical circuits. The combination of immediate visual feedback, scaffolded challenges, and embedded assessment creates an engaging and educationally effective learning environment.

The success of this tool will be measured not just by student engagement, but by demonstrated improvement in conceptual understanding, reduction in persistent misconceptions, and development of scientific thinking skills that transfer beyond the digital environment.

---

*Document Version 1.0 - December 2024*
*Authors: Educational Game Design Team*
*Contact: education@circuitsim3d.edu*