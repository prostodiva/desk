# Pushing a Unity project to a new GitHub repo

Step-by-step reference for putting an **existing Unity project** into a **new
GitHub repository** — as done for the `hands` project →
`https://github.com/prostodiva/gesture-hand-tracking`.

In the commands below, replace:
- `PROJECT` — the Unity project folder (the one that contains `Assets`, `Packages`, `ProjectSettings`), e.g. `/Users/evolvers/dev/vr/hands/hands`
- `REPO_URL` — the GitHub repo URL, e.g. `https://github.com/prostodiva/gesture-hand-tracking.git`

---

## 0. Before you start

- [ ] **Close Unity** or at least save everything (**File → Save**, **File → Save Project**).
- [ ] Make sure the folder is **not already a git repo**:
  ```bash
  cd "PROJECT" && git status
  ```
  `fatal: not a git repository` → good, continue.
  If it **is** a repo already, skip to step 5 and just add the new remote.

---

## 1. Create the repo on GitHub

1. GitHub → **New repository**.
2. Name it; choose Public or Private.
3. **Add .gitignore → template: Unity.** (Recommended — saves step 3b.)
4. Add a README if you like.
5. **Create repository** and copy its URL (`REPO_URL`).

Two cases follow, depending on whether the repo already has files:

| GitHub repo | Follow |
|---|---|
| Has a README and/or `.gitignore` (created with the template) | **Case A** |
| Completely empty | **Case B** |

---

## 2. Turn the project folder into a git repo

```bash
cd "PROJECT" && git init -b main && git remote add origin REPO_URL
```

- `git init -b main` — creates the repo with branch `main` (GitHub's default).
- `git remote add origin REPO_URL` — links it to GitHub under the name `origin`.

---

## 3. Get the .gitignore in place

> **The `.gitignore` must be in the Unity project folder itself** (next to
> `Assets`). Its rules like `/[Ll]ibrary/` only match folders next to it. If it
> sits one level higher, Unity's 2+ GB `Library` cache gets committed.

### 3a. Case A — the GitHub repo already has files
Bring GitHub's commit in without touching your project files:

```bash
cd "PROJECT" && git fetch origin && git reset --mixed origin/main && git checkout origin/main -- .gitignore README.md
```

- `git fetch origin` — downloads GitHub's commit (README, `.gitignore`).
- `git reset --mixed origin/main` — makes your branch start from that commit,
  **keeping all your project files as they are**.
- `git checkout origin/main -- .gitignore README.md` — puts those two files into
  your folder. (Drop `README.md` from the command if the repo has none.)

### 3b. Case B — the GitHub repo is empty
Create the `.gitignore` from GitHub's official Unity template:

```bash
cd "PROJECT" && curl -fsSL https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -o .gitignore
```

### 3c. Both cases — ignore macOS files
```bash
cd "PROJECT" && grep -q "^\.DS_Store" .gitignore || printf '\n# macOS\n.DS_Store\n' >> .gitignore
```

---

## 4. Stage and check before committing

```bash
cd "PROJECT" && git add -A && git status --short | wc -l
```
A typical small project is a few hundred to a couple of thousand files. If you see
**tens of thousands**, something that should be ignored was added — check below.

**Check nothing from Unity's caches was added** (should print nothing):
```bash
cd "PROJECT" && git diff --cached --name-only | grep -E "^(Library|Temp|Logs|UserSettings|obj)/|\.csproj$|\.slnx?$"
```

**Check `Library` is ignored** (should print `Library ignored`):
```bash
cd "PROJECT" && git check-ignore -q Library/ArtifactDB && echo "Library ignored"
```

**Check for large files** (GitHub rejects files over **100 MB**; over 50 MB warns):
```bash
cd "PROJECT" && git diff --cached --name-only -z | xargs -0 du -k 2>/dev/null | sort -rn | head -5
```
Sizes are in KB. Anything near **50000+** → consider Git LFS or leaving it out.

If something is wrong: fix `.gitignore`, then **unstage everything and re-add**:
```bash
cd "PROJECT" && git reset && git add -A
```

---

## 5. Commit

```bash
cd "PROJECT" && git commit -m "Add Unity project"
```

---

## 6. Push

```bash
cd "PROJECT" && git push -u origin main
```

- `-u` links your local `main` to GitHub's `main`, so later a plain `git push` works.
- If asked to log in: use the GitHub CLI (`gh auth login`) or a personal access
  token as the password — GitHub no longer accepts account passwords.

**Check it worked:**
```bash
cd "PROJECT" && git status -sb
```
Expect `## main...origin/main` with nothing ahead. Refresh the repo page on GitHub.

---

## 7. Every time after that

```bash
cd "PROJECT" && git add -A && git commit -m "Describe the change" && git push
```

On another computer:
```bash
git clone REPO_URL
```
Then **Unity Hub → Add → Add project from disk** → choose the cloned folder (the
one containing `Assets`), using the **same Unity version**. The first open
rebuilds `Library` and downloads packages, which takes a few minutes.

---

## Unity habits that keep the repo healthy

- **Always commit the `.meta` files** together with their assets. They hold the
  IDs that scenes and prefabs use; losing them causes "Missing (Mono Script)".
- **Move and rename assets inside Unity**, not in Finder — Unity moves the `.meta` too.
- **Save the scene and project** before committing; unsaved changes aren't on disk.
- Check **Edit → Project Settings → Editor**: *Version Control → Visible Meta
  Files*, *Asset Serialization → Force Text* (Unity's defaults; text makes diffs readable).
- **Before every push, glance at the file count** (`git status --short | wc -l`).
  A sudden jump to thousands usually means a cache or an embedded package slipped in.

---

## Troubleshooting

| Problem | Cause → fix |
|---|---|
| Push rejected: *"Updates were rejected because the remote contains work…"* | GitHub has a commit you don't (Case A skipped). Run `git fetch origin` then `git rebase origin/main`, resolve if needed, push again. |
| Thousands of files staged, `Library/…` among them | `.gitignore` missing or in the wrong folder → put it next to `Assets`, then `git reset && git add -A`. |
| *"File … exceeds GitHub's file size limit of 100 MB"* | Remove it from the commit (`git rm --cached "path"`, commit again) or use **Git LFS**. |
| `remote origin already exists` | `git remote set-url origin REPO_URL` |
| *"src refspec main does not match any"* | Nothing committed yet → run step 5 first. |
| *"Authentication failed"* | Use `gh auth login`, or a personal access token instead of your password. |
| Opened on another PC: scripts "Missing" | `.meta` files weren't committed, or assets were moved outside Unity. |

---

## The commands used for the `hands` project (Case A)

```bash
cd "/Users/evolvers/dev/vr/hands/hands" && git init -b main && git remote add origin https://github.com/prostodiva/gesture-hand-tracking.git
cd "/Users/evolvers/dev/vr/hands/hands" && git fetch origin && git reset --mixed origin/main && git checkout origin/main -- .gitignore README.md
cd "/Users/evolvers/dev/vr/hands/hands" && grep -q "^\.DS_Store" .gitignore || printf '\n# macOS\n.DS_Store\n' >> .gitignore
cd "/Users/evolvers/dev/vr/hands/hands" && git add -A && git status --short | wc -l
cd "/Users/evolvers/dev/vr/hands/hands" && git commit -m "Add hands project with joint-based pinch grabbing"
cd "/Users/evolvers/dev/vr/hands/hands" && git push -u origin main
```
