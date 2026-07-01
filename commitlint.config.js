module.exports = {
  extends: ['@commitlint/config-conventional'],

  parserPreset: {
    parserOpts: {
      headerPattern: /^(\w+)(?:\(([^)]+)\))?!?:\s+(?:(?::[a-z0-9_-]+:|[\p{Emoji_Presentation}\p{Extended_Pictographic}])\s+)?(.+)$/u,
      headerCorrespondence: ['type', 'scope', 'subject']
    }
  },

  rules: {
    'type-enum': [
      2,
      'always',
      [
        'feat',
        'fix',
        'docs',
        'style',
        'refactor',
        'test',
        'chore',
        'build',
        'ci',
        'perf',
        'revert'
      ]
    ],
    'subject-empty': [2, 'never'],
    'subject-case': [0]
  }
};
