import sys
import os
import json
import subprocess

def main():
    project_root = sys.argv[1]
    output_path = sys.argv[2]

    # Step 1 -- File Discovery via git ls-files
    try:
        result = subprocess.run(
            ["git", "ls-files"],
            cwd=project_root,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace"
        )
        all_files = result.stdout.splitlines()
    except Exception as e:
        all_files = []

    # Step 2 -- Exclusion Filtering
    EXCLUDE_DIR_FRAGMENTS = [
        "node_modules/", ".git/", "vendor/", "venv/", ".venv/", "__pycache__/",
        "dist/", "build/", "out/", "coverage/", ".next/", ".cache/", ".turbo/",
        "target/", "obj/", "bin/", ".idea/", ".vscode/", ".vs/"
    ]

    EXCLUDE_EXTENSIONS = {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico",
        ".woff", ".woff2", ".ttf", ".eot", ".mp3", ".mp4",
        ".pdf", ".zip", ".tar", ".gz",
        ".lock", ".map",
        ".md", ".txt", ".yml", ".yaml", ".toml", ".json", ".xml",
        ".cfg", ".ini", ".log", ".sql", ".http"
    }

    EXCLUDE_FILENAMES = {
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml",
        "LICENSE", ".gitignore", ".editorconfig", ".prettierrc",
        "Makefile", "Dockerfile"
    }

    KEEP_EXTENSIONS = {
        ".ts", ".tsx", ".js", ".jsx", ".py", ".go", ".rs", ".java",
        ".rb", ".cpp", ".cc", ".cxx", ".h", ".hpp", ".c", ".cs",
        ".swift", ".kt", ".php", ".vue", ".svelte", ".sh", ".bash"
    }

    LANGUAGE_MAP = {
        ".ts": "typescript", ".tsx": "typescript",
        ".js": "javascript", ".jsx": "javascript",
        ".py": "python",
        ".go": "go",
        ".rs": "rust",
        ".java": "java",
        ".rb": "ruby",
        ".cpp": "cpp", ".cc": "cpp", ".cxx": "cpp", ".h": "cpp", ".hpp": "cpp",
        ".c": "c",
        ".cs": "csharp",
        ".swift": "swift",
        ".kt": "kotlin",
        ".php": "php",
        ".vue": "vue",
        ".svelte": "svelte",
        ".sh": "bash", ".bash": "bash"
    }

    def should_exclude(filepath):
        normalized = filepath.replace("\\", "/")
        for frag in EXCLUDE_DIR_FRAGMENTS:
            if frag in normalized:
                return True
        basename = os.path.basename(normalized)
        if basename in EXCLUDE_FILENAMES:
            return True
        if basename.startswith(".eslintrc"):
            return True
        _, ext = os.path.splitext(basename)
        ext = ext.lower()
        # Exclude generated files
        if basename.endswith(".min.js") or basename.endswith(".min.css"):
            return True
        if basename.endswith(".d.ts"):
            return True
        if ".generated." in basename:
            return True
        if ext in EXCLUDE_EXTENSIONS:
            return True
        if ext not in KEEP_EXTENSIONS:
            return True
        return False

    source_files = []
    for f in all_files:
        if not should_exclude(f):
            source_files.append(f)

    # Step 3 & 4 -- Language Detection and Line Counting
    files_data = []
    for rel_path in sorted(source_files):
        _, ext = os.path.splitext(rel_path)
        ext = ext.lower()
        language = LANGUAGE_MAP.get(ext, "unknown")
        abs_path = os.path.join(project_root, rel_path.replace("/", os.sep))
        try:
            with open(abs_path, "r", encoding="utf-8", errors="replace") as fh:
                lines = fh.readlines()
            size_lines = len(lines)
        except Exception:
            size_lines = 0
        files_data.append({
            "path": rel_path.replace("\\", "/"),
            "language": language,
            "sizeLines": size_lines
        })

    # Step 5 -- Framework Detection
    frameworks = set()
    languages_set = set(f["language"] for f in files_data)

    # Check for .csproj files
    try:
        csproj_result = subprocess.run(
            ["git", "ls-files", "*.csproj"],
            cwd=project_root,
            capture_output=True, text=True, encoding="utf-8", errors="replace"
        )
        csproj_files = [l for l in csproj_result.stdout.splitlines() if l.strip()]
        # Also search recursively
        csproj_result2 = subprocess.run(
            ["git", "ls-files"],
            cwd=project_root,
            capture_output=True, text=True, encoding="utf-8", errors="replace"
        )
        all_tracked = csproj_result2.stdout.splitlines()
        csproj_files = [f for f in all_tracked if f.endswith(".csproj")]
    except Exception:
        csproj_files = []

    if csproj_files:
        languages_set.add("csharp")
        frameworks.add("ASP.NET Core")

    # Check for Aspire in project names
    for f in csproj_files:
        if "Aspire" in f or "aspire" in f:
            frameworks.add(".NET Aspire")
            break

    # Check csproj contents for Aspire
    for csproj in csproj_files:
        abs_path = os.path.join(project_root, csproj.replace("/", os.sep))
        try:
            with open(abs_path, "r", encoding="utf-8", errors="replace") as fh:
                content = fh.read()
            if "Aspire" in content:
                frameworks.add(".NET Aspire")
            if "MassTransit" in content:
                frameworks.add("MassTransit")
            if "MediatR" in content:
                frameworks.add("MediatR")
            if "EntityFramework" in content or "Microsoft.EntityFrameworkCore" in content:
                frameworks.add("Entity Framework Core")
        except Exception:
            pass

    # Scan .cs files for framework patterns
    masstransit_found = False
    mediatr_found = False
    ef_found = False
    for fdata in files_data:
        if fdata["language"] != "csharp":
            continue
        abs_path = os.path.join(project_root, fdata["path"].replace("/", os.sep))
        try:
            with open(abs_path, "r", encoding="utf-8", errors="replace") as fh:
                content = fh.read()
            if not masstransit_found and "MassTransit" in content:
                frameworks.add("MassTransit")
                masstransit_found = True
            if not mediatr_found and ("MediatR" in content or "IMediator" in content):
                frameworks.add("MediatR")
                mediatr_found = True
            if not ef_found and ("DbContext" in content or "EntityFrameworkCore" in content):
                frameworks.add("Entity Framework Core")
                ef_found = True
        except Exception:
            pass
        if masstransit_found and mediatr_found and ef_found:
            break

    # Step 6 -- Complexity Estimation
    total = len(files_data)
    if total <= 20:
        complexity = "small"
    elif total <= 100:
        complexity = "moderate"
    elif total <= 500:
        complexity = "large"
    else:
        complexity = "very-large"

    # Step 7 -- Project Name
    project_name = os.path.basename(os.path.abspath(project_root))

    output = {
        "scriptCompleted": True,
        "name": project_name,
        "rawDescription": "",
        "readmeHead": "",
        "languages": sorted(list(languages_set)),
        "frameworks": sorted(list(frameworks)),
        "files": files_data,
        "totalFiles": total,
        "estimatedComplexity": complexity
    }

    with open(output_path, "w", encoding="utf-8") as fh:
        json.dump(output, fh, indent=2, ensure_ascii=False)

    print(f"Done. {total} source files written to {output_path}")
    sys.exit(0)

if __name__ == "__main__":
    main()
