# NPM Package Setup Guide

This guide explains how to complete the setup for automatic npm publishing.

## Overview

This repository now contains an npm package named `@willh/html-text-node-parser` that will automatically publish to the npm registry when:
1. Commits are pushed to the `main` branch
2. The version in `package.json` differs from the published version on npm

## Required Configuration

To enable automatic publishing, you need to configure an npm access token:

### Step 1: Create NPM Access Token

1. Log in to your npm account at https://www.npmjs.com/
2. Click on your profile picture → Access Tokens
3. Click "Generate New Token" → "Classic Token"
4. Select "Automation" type (for CI/CD use)
5. Copy the generated token (you won't see it again!)

### Step 2: Add Token to GitHub

1. Go to your repository on GitHub: https://github.com/doggy8088/cdc-data
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Name: `NPM_TOKEN`
5. Secret: Paste your npm access token
6. Click "Add secret"

## How It Works

The GitHub Actions workflow (`.github/workflows/npm-publish.yml`) will:

1. **Trigger**: Runs on every push to the `main` branch
2. **Build**: Compiles TypeScript to JavaScript
3. **Version Check**: Compares local version with npm registry
4. **Publish**: Only publishes if versions differ

## Publishing a New Version

To publish a new version:

1. Update the version in `package.json`:
   ```json
   {
     "version": "1.0.1"  // Increment as needed
   }
   ```

2. Commit and push to `main`:
   ```bash
   git add package.json
   git commit -m "Bump version to 1.0.1"
   git push origin main
   ```

3. The workflow will automatically publish to npm

## Version Naming Convention

Follow [Semantic Versioning](https://semver.org/):
- **MAJOR** (1.x.x): Breaking changes
- **MINOR** (x.1.x): New features, backward compatible
- **PATCH** (x.x.1): Bug fixes, backward compatible

## Package Structure

```
.
├── src/              # TypeScript source files
│   └── index.ts      # Main package entry point
├── dist/             # Compiled JavaScript (generated)
│   ├── index.js      # Compiled output
│   └── index.d.ts    # TypeScript definitions
├── package.json      # Package configuration
├── tsconfig.json     # TypeScript configuration
├── NPM_README.md     # Package documentation for npm
└── .github/
    └── workflows/
        └── npm-publish.yml  # Publishing workflow
```

## Files Included in NPM Package

The published package includes:
- `dist/` - Compiled JavaScript and TypeScript definitions
- `NPM_README.md` - Package documentation
- `package.json` - Package metadata

## Files Excluded from NPM Package

The following are NOT published to npm (via `.npmignore`):
- Source TypeScript files (`src/`)
- Development configuration
- GitHub workflows
- Repository data files
- Main README.md (CDC data documentation)

## Testing Locally

Before publishing, you can test the package locally:

```bash
# Install dependencies
npm install

# Build the package
npm run build

# Check what will be published
npm pack --dry-run

# Test in another project
npm pack
# Then install the .tgz file in another project
```

## Troubleshooting

### Workflow fails with authentication error
- Verify `NPM_TOKEN` secret is configured correctly
- Check that the token has publish permissions
- Ensure the token hasn't expired

### Version already exists error
- Update the version number in `package.json`
- NPM doesn't allow republishing the same version

### Build fails
- Check TypeScript compilation errors in the workflow logs
- Test locally with `npm run build`

## Support

For issues related to:
- **Package functionality**: Open an issue in this repository
- **NPM publishing**: Check GitHub Actions workflow logs
- **Package usage**: See `NPM_README.md` for documentation
