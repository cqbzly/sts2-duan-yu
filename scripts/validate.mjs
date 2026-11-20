import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { renderCards } from './build-cards.mjs';

const rarityDefinitions = [
  { rarity: '基础', prefix: 'B', count: 5 },
  { rarity: '普通', prefix: 'C', count: 23 },
  { rarity: '罕见', prefix: 'U', count: 38 },
  { rarity: '稀有', prefix: 'R', count: 22 },
  { rarity: '衍生', prefix: 'T', count: 6 },
  { rarity: '状态', prefix: 'S', count: 2 },
];
const allowedTypes = new Set(['攻击', '技能', '能力', '状态']);
const allowedTags = new Set(['北冥', '凌波', '六脉', '通用']);
const requiredQiCosts = new Map([
  ['DY_T01', 1], ['DY_T02', 1], ['DY_T03', 1], ['DY_T04', 1], ['DY_T05', 1], ['DY_T06', 1],
  ['DY_C17', 1],
  ['DY_B05', 2], ['DY_C15', 2], ['DY_U32', 2], ['DY_R06', 2],
  ['DY_C14', 3], ['DY_U07', 3], ['DY_U17', 3], ['DY_U33', 3], ['DY_R03', 3], ['DY_R05', 3], ['DY_R20', 3],
  ['DY_R13', 5],
]);
const errors = [];

function requireCondition(condition, message) {
  if (!condition) errors.push(message);
}

function isObject(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function isNonemptyText(value) {
  return typeof value === 'string' && value.trim().length > 0;
}

function isCost(value, cardId) {
  return Number.isInteger(value) && (value >= 0 && value <= 3 || cardId === 'DY_S01' && value === -1);
}

function validateEffect(text, label, expectedQiCost = 0) {
  requireCondition(isNonemptyText(text), `${label}必须提供完整效果文本。`);
  if (!isNonemptyText(text)) return;
  requireCondition(!/\?{2,}|\uFFFD/.test(text), `${label}含有疑似编码损坏的连续问号或替换字符。`);
  requireCondition(
    !/^(同上|同原卡|同基础|与基础相同|无变化)[。.]?$/.test(text.trim()) &&
      !/(其余|其他)效果不变/.test(text),
    `${label}不能用省略式说明替代完整文本。`,
  );
  const obsoleteSpendingClauses = [...text.matchAll(/耗气\s*(\d+)\s*[：:]/g)];
  requireCondition(obsoleteSpendingClauses.length === 0, `${label}仍使用旧的条件耗气格式；应改为出牌前的“需支付N点真气”。`);
  const requiredQiClauses = [...text.matchAll(/需(?:支付)?\s*(\d+)\s*点真气/g)];
  requireCondition(requiredQiClauses.length <= 1, `${label}最多只能有一条真气费用。`);
  for (const clause of requiredQiClauses) {
    const amount = Number(clause[1]);
    requireCondition(Number.isSafeInteger(amount) && amount > 0, `${label}的真气费用必须是正整数。`);
    requireCondition(expectedQiCost === 0 || amount === expectedQiCost, `${label}的真气费用应为${expectedQiCost}点。`);
  }
  requireCondition(expectedQiCost === 0 || requiredQiClauses.length === 1, `${label}必须明确写出需支付${expectedQiCost}点真气。`);
}

function validateCharacter(character, cardsById) {
  requireCondition(isObject(character), 'character必须是对象。');
  if (!isObject(character)) return;
  requireCondition(character.id === 'duan_yu', '角色id必须为duan_yu。');
  requireCondition(isNonemptyText(character.name), '角色必须有名称。');
  for (const field of ['maxHp', 'energy', 'draw', 'lingboCap', 'lingboReduction']) {
    requireCondition(Number.isInteger(character[field]) && character[field] > 0, `character.${field}必须是正整数。`);
  }
  requireCondition(character.qiCap === null, 'character.qiCap必须为null，表示真气无上限。');
  requireCondition(Number.isInteger(character.gold) && character.gold >= 0, '初始金币必须是非负整数。');
  requireCondition(Array.isArray(character.startingDeck), 'startingDeck必须是数组。');
  if (!Array.isArray(character.startingDeck)) return;
  const startingIds = new Set();
  let startingCount = 0;
  for (const entry of character.startingDeck) {
    requireCondition(isObject(entry), '初始牌组条目必须是对象。');
    if (!isObject(entry)) continue;
    requireCondition(!startingIds.has(entry.id), `初始牌组重复列出${entry.id}。`);
    startingIds.add(entry.id);
    requireCondition(cardsById.get(entry.id)?.rarity === '基础', `初始牌组的${entry.id}必须指向基础牌。`);
    requireCondition(Number.isInteger(entry.count) && entry.count > 0, `初始牌组${entry.id}的数量必须是正整数。`);
    if (Number.isInteger(entry.count) && entry.count > 0) startingCount += entry.count;
  }
  requireCondition(startingCount === 10, `初始牌组应有10张，实际为${startingCount}张。`);
}

function validateCards(design) {
  requireCondition(Array.isArray(design.cards), 'cards必须是数组。');
  if (!Array.isArray(design.cards)) return;
  const cardsById = new Map();
  const names = new Set();
  const expectedRarities = new Map();
  for (const definition of rarityDefinitions) {
    for (let sequence = 1; sequence <= definition.count; sequence += 1) {
      const cardId = `DY_${definition.prefix}${String(sequence).padStart(2, '0')}`;
      expectedRarities.set(cardId, definition.rarity);
    }
    const actualCount = design.cards.filter((card) => isObject(card) && card.rarity === definition.rarity).length;
    requireCondition(actualCount === definition.count, `${definition.rarity}牌应有${definition.count}种，实际为${actualCount}种。`);
  }
  requireCondition(design.cards.length === 96, `卡牌总数应为96种，实际为${design.cards.length}种。`);

  for (const [index, card] of design.cards.entries()) {
    const label = isObject(card) && isNonemptyText(card.id) ? card.id : `cards[${index}]`;
    requireCondition(isObject(card), `${label}必须是对象。`);
    if (!isObject(card)) continue;
    requireCondition(expectedRarities.has(card.id), `${label}不是预定范围内的卡牌ID。`);
    requireCondition(!cardsById.has(card.id), `${label}的ID重复。`);
    cardsById.set(card.id, card);
    requireCondition(expectedRarities.get(card.id) === card.rarity, `${label}的稀有度与ID不符。`);
    requireCondition(isNonemptyText(card.name) && card.name === card.name.trim(), `${label}必须有无首尾空白的名称。`);
    requireCondition(!names.has(card.name), `${label}的名称“${card.name}”重复。`);
    names.add(card.name);
    requireCondition(allowedTypes.has(card.type), `${label}的类型必须为攻击、技能、能力或状态。`);
    requireCondition(isCost(card.cost, card.id), `${label}的费用必须是0至3的整数，只有气滞允许-1。`);
    requireCondition(card.costX === undefined || typeof card.costX === 'boolean', `${label}.costX必须为布尔值。`);
    requireCondition(Boolean(card.costX) === (card.id === 'DY_U37'), `${label}的能量X费用配置不正确。`);
    requireCondition(card.qiCostX === undefined || typeof card.qiCostX === 'boolean', `${label}.qiCostX必须为布尔值。`);
    requireCondition(Boolean(card.qiCostX) === (card.id === 'DY_R21'), `${label}的真气X费用配置不正确。`);
    if (card.qiCostX) {
      requireCondition(card.cost === 0 && card.upgrade?.cost === 0 && !card.costX, `${label}的真气X费用牌必须为0能量费。`);
      for (const text of [card.text, card.upgrade?.text]) {
        requireCondition(typeof text === 'string' && text.includes('消耗全部真气（X）'), `${label}必须明确说明真气X消耗。`);
      }
    }
    if (card.costX) requireCondition(card.cost === 0 && card.upgrade?.cost === 0, `${label}的X费用占位值必须为0。`);
    validateEffect(card.text, `${label}基础效果`, requiredQiCosts.get(card.id) ?? 0);
    requireCondition(isObject(card.upgrade), `${label}必须有upgrade对象。`);
    if (isObject(card.upgrade)) {
      requireCondition(isCost(card.upgrade.cost, card.id), `${label}的升级费用必须是0至3的整数，只有气滞允许-1。`);
      validateEffect(card.upgrade.text, `${label}升级效果`, requiredQiCosts.get(card.id) ?? 0);
    }
    requireCondition(Array.isArray(card.tags) && card.tags.length >= 1 && card.tags.length <= 2, `${label}必须有1至2个标签。`);
    if (Array.isArray(card.tags)) {
      requireCondition(new Set(card.tags).size === card.tags.length, `${label}有重复标签。`);
      for (const tag of card.tags) requireCondition(allowedTags.has(tag), `${label}存在未知标签“${tag}”。`);
    }
    if (card.generates !== undefined) {
      requireCondition(Array.isArray(card.generates), `${label}.generates必须是数组。`);
      if (Array.isArray(card.generates)) {
        requireCondition(new Set(card.generates).size === card.generates.length, `${label}.generates有重复ID。`);
      }
    }
    if (card.rarity === '衍生') {
      requireCondition(card.type === '攻击' && card.cost === 0 && card.upgrade?.cost === 0, `${label}必须是基础及升级均为0费的攻击牌。`);
      for (const [variant, text] of [['基础', card.text], ['升级', card.upgrade?.text]]) {
        requireCondition(typeof text === 'string' && text.includes('消耗') && text.includes('虚无'), `${label}${variant}文本必须包含消耗和虚无。`);
      }
    }
    requireCondition((card.rarity === '状态') === (card.type === '状态'), `${label}的状态类型必须与状态分类对应。`);
    if (card.rarity === '状态') {
      requireCondition(card.upgrade?.cost === card.cost && card.upgrade?.text === card.text, `${label}不可升级，upgrade必须重复基础费用与文本。`);
      requireCondition(card.cost === (card.id === 'DY_S01' ? -1 : 1), `${label}状态牌费用不正确。`);
      requireCondition(typeof card.text === 'string' && card.text.includes(card.id === 'DY_S01' ? '虚无' : '消耗'), `${label}缺少状态牌离场关键词。`);
    }
  }

  for (const expectedId of expectedRarities.keys()) {
    requireCondition(cardsById.has(expectedId), `缺少卡牌${expectedId}。`);
  }
  for (const card of cardsById.values()) {
    if (!Array.isArray(card.generates)) continue;
    for (const generatedId of card.generates) {
      requireCondition(['衍生', '状态'].includes(cardsById.get(generatedId)?.rarity), `${card.id}.generates的${generatedId}必须指向存在的衍生剑气或状态牌。`);
    }
  }
  const effectOwners = new Map();
  for (const card of cardsById.values()) {
    // Different costs cannot give identical base and upgraded effects distinct roles.
    const signature = JSON.stringify([card.type, card.text, card.upgrade?.text]);
    const previousId = effectOwners.get(signature);
    requireCondition(!previousId, `${card.id}与${previousId}的基础及升级效果完全重复，请区分卡牌定位。`);
    effectOwners.set(signature, card.id);
  }
  validateCharacter(design.character, cardsById);
}

async function main() {
  const sourcePath = fileURLToPath(new URL('../data/cards.json', import.meta.url));
  const outputPath = fileURLToPath(new URL('../docs/cards.md', import.meta.url));
  const design = JSON.parse(await readFile(sourcePath, 'utf8'));
  requireCondition(isObject(design), '设计数据根节点必须是对象。');
  if (isObject(design)) {
    requireCondition(typeof design.version === 'string' && /^\d+\.\d+\.\d+$/.test(design.version), 'version必须是三段数字版本号。');
    validateCards(design);
  }
  if (errors.length === 0) {
    try {
      const actualMarkdown = await readFile(outputPath, 'utf8');
      requireCondition(
        actualMarkdown.replaceAll('\r\n', '\n') === renderCards(design),
        'docs/cards.md与数据不一致，请运行node scripts/build-cards.mjs。',
      );
    } catch (error) {
      if (error.code !== 'ENOENT') throw error;
      errors.push('缺少docs/cards.md，请运行node scripts/build-cards.mjs。');
    }
  }
  if (errors.length > 0) {
    console.error(`设计数据校验失败（${errors.length}项）：\n${errors.map((message) => `- ${message}`).join('\n')}`);
    process.exitCode = 1;
    return;
  }
  console.log('设计数据校验通过：96种卡牌（基础5、普通23、罕见38、稀有22、衍生6、状态2），10张初始牌组，生成引用及卡牌文档一致。');
  console.log('本校验仅检查设计数据结构及文档一致性，不代表实战平衡或游戏模组兼容性通过。');
}

main().catch((error) => {
  console.error(`设计数据校验失败：${error.message}`);
  process.exitCode = 1;
});
