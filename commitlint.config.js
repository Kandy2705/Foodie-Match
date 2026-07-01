module.exports = {
  extends: ['@commitlint/config-conventional'],
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
    'scope-enum': [
      1,
      'always',
      [
        'screen',
        'button',
        'gameplay',
        'tray',
        'food',
        'plate',
        'order',
        'serving',
        'storage',
        'level',
        'booster',
        'data',
        'animation',
        'vfx',
        'audio',
        'tester',
        'project',
        'readme'
      ]
    ],
    'subject-empty': [2, 'never'],
    'subject-case': [0]
  }
};
