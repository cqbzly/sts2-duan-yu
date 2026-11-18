import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const rarityGroups = [
  { rarity: '基础', label: '基础牌', anchor: 'basic-cards' },
  { rarity: '普通', label: '普通牌', anchor: 'common-cards' },
  { rarity: '罕见', label: '罕见牌', anchor: 'uncommon-cards' },
  { rarity: '稀有', label: '稀有牌', anchor: 'rare-cards' },
  { rarity: '衍生', label: '衍生剑气', anchor: 'generated-cards' },
];

function inlineText(value) {
  return String(value).replaceAll('|', '\\|').replace(/\r?\n/g, '<br>');
}

export function renderCards(design) {
  const cardsById = new Map(design.cards.map((card) => [card.id, card]));
  const groups = rarityGroups.map((group) => ({
    ...group,
    cards: design.cards
      .filter((card) => card.rarity === group.rarity)
      .sort((left, right) => left.id.localeCompare(right.id, 'en')),
  }));
  const obtainableCount = design.cards.filter((card) =>
    ['普通', '罕见', '稀有'].includes(card.rarity),
  ).length;
  const lines = [
    `# ${design.character.name} · 完整卡牌图鉴`,
    '',
    `设计版本：${design.version}。共 ${design.cards.length} 种卡牌，其中 ${obtainableCount} 种进入角色常规卡池。`,
    '',
    '本文由 [data/cards.json](../data/cards.json) 自动生成。修改数据后运行 `node scripts/build-cards.mjs`；运行 `node scripts/validate.mjs` 检查数据及文档一致性。',
    '',
    '每张牌均提供完整基础效果和完整升级效果；升级栏是升级后整张牌的文本。费用均为能量费用。衍生关联只描述该牌能够生成的卡种，不代表数量、升级状态或实际生成顺序，这些以效果文本为准。',
    '',
    '## 目录',
    '',
    ...groups.map((group) => `- [${group.label}（${group.cards.length}）](#${group.anchor})`),
    '',
  ];

  for (const group of groups) {
    lines.push(
      `<a id="${group.anchor}"></a>`,
      '',
      `## ${group.label}（${group.cards.length}）`,
      '',
      group.cards.map((card) => `[${card.name}](#${card.id.toLowerCase()})`).join(' · '),
      '',
    );

    for (const card of group.cards) {
      const generatedLinks = (card.generates ?? []).map((generatedId) => {
        const generatedCard = cardsById.get(generatedId);
        return `[${generatedCard?.name ?? generatedId}（${generatedId}）](#${generatedId.toLowerCase()})`;
      });
      lines.push(
        `<a id="${card.id.toLowerCase()}"></a>`,
        '',
        `### ${card.name}`,
        '',
        '| 编号 | 类型 | 基础费用 | 升级费用 | 标签 |',
        '| --- | --- | --- | --- | --- |',
        `| ${card.id} | ${card.type} | ${card.cost} | ${card.upgrade.cost} | ${card.tags.map(inlineText).join('、')} |`,
        '',
        `**基础效果：** ${inlineText(card.text)}`,
        '',
        `**完整升级效果：** ${inlineText(card.upgrade.text)}`,
        '',
        `**衍生关联：** ${generatedLinks.length > 0 ? generatedLinks.join('、') : '无。'}`,
        '',
      );
    }
  }

  lines.push('---', '', '本图鉴是角色设计规格，不代表经过实战平衡测试或游戏模组兼容性验证。', '');
  return lines.join('\n');
}

async function main() {
  const sourcePath = fileURLToPath(new URL('../data/cards.json', import.meta.url));
  const outputPath = fileURLToPath(new URL('../docs/cards.md', import.meta.url));
  const design = JSON.parse(await readFile(sourcePath, 'utf8'));
  await mkdir(dirname(outputPath), { recursive: true });
  await writeFile(outputPath, renderCards(design), 'utf8');
  console.log(`已生成 docs/cards.md：${design.cards.length} 种卡牌，版本 ${design.version}。`);
}

if (process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url) {
  main().catch((error) => {
    console.error(`卡牌文档生成失败：${error.message}`);
    process.exitCode = 1;
  });
}
