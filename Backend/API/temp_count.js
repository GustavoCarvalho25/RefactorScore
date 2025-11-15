const fs = require('fs');

const content = fs.readFileSync('CommitAnalysis.json', 'utf-8');
const lines = content.trim().split('\n');

let totalSugestoesFiles = 0;
let totalArquivos = 0;
let detalhes = [];

lines.forEach((line) => {
  try {
    const obj = JSON.parse(line);
    if (obj.Files && Array.isArray(obj.Files)) {
      let sugestoesNesseCommit = 0;
      totalArquivos += obj.Files.length;
      
      obj.Files.forEach(file => {
        if (file.Suggestions && Array.isArray(file.Suggestions)) {
          sugestoesNesseCommit += file.Suggestions.length;
          totalSugestoesFiles += file.Suggestions.length;
        }
      });
      
      detalhes.push({
        commit: obj.CommitId ? obj.CommitId.substring(0, 8) : 'N/A',
        arquivos: obj.Files.length,
        sugestoes: sugestoesNesseCommit
      });
    }
  } catch(e) {
    // Ignora linhas com erro
  }
});

console.log('=== RESUMO DAS SUGESTOES ===\n');
detalhes.forEach(d => {
  console.log(`Commit ${d.commit}: ${d.arquivos} arquivo(s) com ${d.sugestoes} sugestao(oes)`);
});
console.log(`\nTOTAL: ${totalSugestoesFiles} sugestoes em ${totalArquivos} arquivos`);

