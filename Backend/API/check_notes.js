const fs = require('fs');

// Le o arquivo e busca todas as ocorrencias de OverallNote
const content = fs.readFileSync('CommitAnalysis.json', 'utf-8');

console.log('=== VERIFICANDO NOTAS DOS COMMITS NO ARQUIVO ===\n');

// Busca por linhas com OverallNote no nivel do documento (nao dentro de arrays)
const lines = content.split('\n');
const overallNotes = [];
let inFiles = false;
let braceCount = 0;

for (let i = 0; i < lines.length; i++) {
  const line = lines[i].trim();
  
  // Detecta se estamos dentro do array Files
  if (line.includes('Files: [')) {
    inFiles = true;
    braceCount = 0;
  }
  
  // Conta chaves para saber quando sair do Files
  if (inFiles) {
    braceCount += (line.match(/{/g) || []).length;
    braceCount -= (line.match(/}/g) || []).length;
    if (line.includes('],') && braceCount === 0) {
      inFiles = false;
    }
  }
  
  // Se encontrar OverallNote fora do Files
  if (!inFiles && line.includes('OverallNote:')) {
    const match = line.match(/OverallNote:\s*(\d+)/);
    if (match) {
      overallNotes.push(parseInt(match[1]));
      console.log(`Encontrado na linha ${i + 1}: OverallNote = ${match[1]}`);
    }
  }
}

console.log(`\n=== RESULTADO ===`);
console.log(`Total de commits: ${overallNotes.length}`);
console.log(`Notas: ${overallNotes.join(', ')}`);
console.log(`Soma: ${overallNotes.reduce((a, b) => a + b, 0)}`);
console.log(`Media calculada: ${overallNotes.length > 0 ? (overallNotes.reduce((a, b) => a + b, 0) / overallNotes.length).toFixed(2) : 0}`);

