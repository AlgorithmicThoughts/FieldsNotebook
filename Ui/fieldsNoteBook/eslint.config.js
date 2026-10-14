import js from '@eslint/js'
import eslintReact from '@eslint-react/eslint-plugin'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

/** A bad practice should stop the build, so every rule a preset enables reports as an error. */
function asErrors(rules) {
  return Object.fromEntries(Object.entries(rules).map(([name, level]) => [name, level === 'off' ? 'off' : 'error']))
}

const hooksRules = reactHooks.configs['recommended-latest'].rules
const reactRules = eslintReact.configs['strict-type-checked'].rules

/**
 * @eslint-react re-implements several React Compiler rules. The official
 * react-hooks plugin owns those, so its copies are off to avoid double reports.
 */
const OWNED_BY_REACT_HOOKS = [
  'error-boundaries',
  'exhaustive-deps',
  'purity',
  'rules-of-hooks',
  'set-state-in-effect',
  'set-state-in-render',
  'static-components',
  'unsupported-syntax',
  'use-memo',
]

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactRefresh.configs.vite,
    ],
    plugins: {
      'react-hooks': reactHooks,
      ...eslintReact.configs['strict-type-checked'].plugins,
    },
    settings: eslintReact.configs['strict-type-checked'].settings,
    languageOptions: {
      globals: globals.browser,
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    rules: {
      // React Compiler and Rules of Hooks: purity, refs, immutability, effects, memoization.
      ...asErrors(hooksRules),
      // Components, JSX, keys, DOM safety, leaked subscriptions, React 19 APIs.
      ...asErrors(reactRules),
      ...Object.fromEntries(OWNED_BY_REACT_HOOKS.map((rule) => [`@eslint-react/${rule}`, 'off'])),
      // Async event handlers and effects: a rejected promise must be handled, not dropped.
      '@typescript-eslint/no-floating-promises': 'error',
      '@typescript-eslint/no-misused-promises': 'error',
      // Every status of a union such as ApiQueryState must be handled when switched on.
      '@typescript-eslint/switch-exhaustiveness-check': 'error',
      'react-refresh/only-export-components': 'error',
    },
  },
  {
    // Generated shadcn registry components: they export variant helpers, and a
    // regenerate would undo local edits, so only these upstream patterns are allowed.
    files: ['src/components/ui/**/*.{ts,tsx}'],
    rules: {
      'react-refresh/only-export-components': 'off',
      '@eslint-react/no-array-index-key': 'off',
      '@eslint-react/no-leaked-conditional-rendering': 'off',
    },
  },
])
