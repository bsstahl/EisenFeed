# EisenFeed

EisenFeed is an intelligent RSS triage system that applies the **Eisenhower Matrix** (Urgent vs. Important) to information streams. Instead of treating all feed items equally, EisenFeed continuously evaluates, scores, and *re-scores* content over time—surfacing what matters, deferring what can wait, and filtering out the noise.

EisenFeed is designed to integrate with **EchoDrop**, enabling high‑value items to be scheduled as social posts or queued for later publishing.

## 🚀 Core Idea

Traditional RSS readers are chronological. EisenFeed is **priority‑driven**.

Every feed item is evaluated along two axes:

- **Urgency** — How time‑sensitive is this item?
- **Importance** — How aligned is this item with your goals, interests, or trusted sources?

These scores place each item into one of four quadrants:

1. **Urgent & Important** — Read now  
2. **Not Urgent & Important** — Read later  
3. **Urgent & Not Important** — Optional  
4. **Not Urgent & Not Important** — Ignore or auto‑filter  

Unlike static categorization, EisenFeed uses **Content Aging** to adjust scores over time.

## 🧬 Content Aging

EisenFeed models the natural decay of attention:

- **Urgency decays quickly** unless reinforced  
- **Importance decays slowly** (or not at all for evergreen or VIP sources)

This allows items to move between quadrants as their relevance changes:

- Breaking news cools off  
- Long‑form essays remain valuable  
- Low‑value posts fade away  
- Critical alerts stay visible longer  

This creates a dynamic, self‑organizing feed that reflects real‑world attention patterns.

## 🏷️ Feed Modifiers

Not all feeds are equal. EisenFeed supports **modifiers** that influence scoring and decay:

### **Owned Feeds**

Your own blogs, projects, or publication channels  

- Higher importance  
- Slightly higher urgency  
- Slower decay  

### **VIP Feeds**

Friends, colleagues, trusted authors  

- Higher importance  
- Slower importance decay  

### **Government / Critical Notifications**

Emergency alerts, official channels  

- Strong urgency boost  
- Slower urgency decay  
- Auto‑promote to high‑visibility states  

### **Low‑Value / Entertainment Feeds**

Fun but nonessential  

- Lower importance  
- Faster decay  

Modifiers allow EisenFeed to reflect your social graph and personal priorities.

## 🔄 EchoDrop Integration

EisenFeed can pass selected items directly into **EchoDrop**, enabling:

- “Post Now” for high‑urgency items  
- “Schedule for Later” for evergreen content  
- Automated suggestions based on Q2 (Important but Not Urgent) items  
- A unified workflow for reading → selecting → publishing  

EchoDrop becomes the output system; EisenFeed becomes the input triage system.

## 🏗️ Architecture Overview

### **1. Ingestion Layer**

- RSS/Atom parsing  
- Metadata normalization  
- Feed‑level modifier application  

### **2. Scoring Engine**

- Initial urgency/importance scoring  
- NLP‑based topic relevance (future)  
- Modifier multipliers  
- Content Aging functions  

### **3. Quadrant Assignment**

- Continuous scoring  
- Threshold‑based quadrant mapping  
- Time‑based reclassification  

### **4. Display Layer**

- Quadrant‑sorted views  
- Digest mode for Q2  
- Noise‑filtered mode for Q3/Q4  
- Visual aging indicators  

### **5. EchoDrop Integration**

- Manual or automatic scheduling  
- Content suggestions  
- Cross‑system metadata sharing  

## Aspire Workspace Setup

This repository now includes an Aspire AppHost at `src/EisenFeed.AppHost/` for local orchestration.

- AppHost project: `src/EisenFeed.AppHost/EisenFeed.AppHost.csproj`
- Core library project: `src/EisenFeed.Core/EisenFeed.Core.csproj`

Current status:

- Aspire is initialized and the AppHost is part of the solution.
- The AppHost is currently a minimal stub (`Build().Run()`).
- `EisenFeed.Core` is a class library, so there is not yet a runnable API/worker resource to attach.

Run locally with `dotnet`:

```powershell
dotnet restore
dotnet build EisenFeed.slnx
dotnet run --project .\src\EisenFeed.AppHost\EisenFeed.AppHost.csproj
```

Next step to fully wire Aspire resources:

- Add a runnable service project (for example, an ASP.NET Core API or Worker).
- Reference that project from the AppHost with `builder.AddProject(...)`.
- Optionally add data resources such as Postgres or Redis and wire `WithReference(...)`/`WaitFor(...)`.

## Repository Layout Conventions

- Production code MUST live under `src/`.
- Test code MUST live under `tst/`.
- New projects should follow this pattern:
	- `src/<ProjectName>/...`
	- `tst/<ProjectName>.Tests/...`

## 📦 Status

EisenFeed is currently in active design and early prototyping.  
Contributions, ideas, and discussions are welcome.

## 🛣️ Roadmap

- [ ] Core RSS ingestion  
- [ ] Scoring engine (initial implementation)  
- [ ] Content Aging model  
- [ ] Feed modifiers  
- [ ] Quadrant UI  
- [ ] EchoDrop integration  
- [ ] Machine‑learned importance modeling  
- [ ] Multi‑persona profiles  
- [ ] Attention budgeting  

## 🤝 Contributing

Contributions are encouraged!  
Open an issue or submit a PR with ideas, improvements, or feedback.

## 📄 License

TBD

## 💬 About the Project

EisenFeed is built around a simple belief:  
**Information should be prioritized, not just consumed.**

By combining the Eisenhower Matrix with dynamic scoring and social‑aware modifiers, EisenFeed helps you focus on what matters—while ignoring what doesn’t.
