<template>
  <div class="project-selection-page">
    <div class="welcome-section">
      <h1 class="welcome-title">Bem-vindo ao RefactorScore</h1>
      <p class="welcome-subtitle">Análise de Código com Clean Code</p>
      <p class="welcome-description">
        Selecione um projeto abaixo para visualizar suas análises e métricas de qualidade de código
      </p>
    </div>

    <div class="selection-container">
      <div v-if="loading" class="loading-state">
        <div class="spinner"></div>
        <p>Carregando projetos...</p>
      </div>
      
      <div v-else-if="error" class="error-state">
        <p>Erro ao carregar projetos: {{ error }}</p>
        <button @click="loadProjects" class="retry-btn">Tentar Novamente</button>
      </div>
      
      <div v-else-if="projects.length === 0" class="empty-state">
        <h2>Nenhum Projeto Encontrado</h2>
        <p>Execute análises de commits para visualizar projetos aqui.</p>
      </div>
      
      <div v-else class="projects-grid">
        <div 
          v-for="project in projects" 
          :key="project.name"
          class="project-card"
          @click="selectProject(project.name)"
        >
          <div class="project-info">
            <h2 class="project-name">{{ project.name }}</h2>
            <span class="language-badge">{{ project.mainLanguage }}</span>
          </div>
          
          <div class="project-metrics">
            <div class="metric">
              <div class="metric-content">
                <span class="metric-value">{{ project.totalCommits }}</span>
                <span class="metric-label">Commits</span>
              </div>
            </div>
            
            <div class="metric">
              <div class="metric-content">
                <span :class="['metric-value', getScoreClass(project.averageNote)]">
                  {{ project.averageNote.toFixed(1) }}
                </span>
                <span class="metric-label">Nota Média</span>
              </div>
            </div>
            
            <div class="metric">
              <div class="metric-content">
                <span class="metric-value">{{ project.totalFiles }}</span>
                <span class="metric-label">Arquivos</span>
              </div>
            </div>
          </div>
          
          <div class="project-footer">
            <span class="last-analysis">
              Última análise: {{ formatDate(project.lastAnalysisDate) }}
            </span>
          </div>
          
          <div class="select-overlay">
            <span class="select-text">Analisar →</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useProjectStore } from '../stores/projectStore';
import { useProjectService } from '../server/api/projectService';
import type { Project } from '../interfaces/Project';

const router = useRouter();
const projectStore = useProjectStore();
const projectService = useProjectService();

const projects = ref<Project[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

const getScoreClass = (score: number): string => {
  if (score >= 8) return 'excellent';
  if (score >= 6) return 'good';
  if (score >= 4) return 'acceptable';
  return 'needs-improvement';
};

const formatDate = (dateString: string | null): string => {
  if (!dateString) return 'Nunca';
  
  const date = new Date(dateString);
  const now = new Date();
  const diffTime = Math.abs(now.getTime() - date.getTime());
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  
  if (diffDays === 0) return 'Hoje';
  if (diffDays === 1) return 'Ontem';
  if (diffDays < 7) return `${diffDays} dias atrás`;
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} semanas atrás`;
  
  return date.toLocaleDateString('pt-BR');
};

const selectProject = (projectName: string) => {
  projectStore.selectProject(projectName);
  router.push({ name: 'Home' });
};

const loadProjects = async () => {
  loading.value = true;
  error.value = null;
  
  try {
    const response = await projectService.getProjects();
    
    if (response.error.value) {
      error.value = response.error.value.message || 'Erro ao carregar projetos';
    } else if (response.result.value) {
      const data = response.result.value as any;
      projects.value = data.projects || data;
      projectStore.setProjects(projects.value);
    }
  } catch (err: any) {
    error.value = err.message || 'Erro desconhecido';
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadProjects();
});
</script>

<style scoped lang="scss">
.project-selection-page {
  min-height: 100vh;
  background: var(--background-color);
  padding: 3rem 2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.welcome-section {
  text-align: center;
  color: var(--text-primary);
  margin-bottom: 3rem;
  max-width: 800px;
}

.welcome-title {
  font-size: 2.5rem;
  font-weight: 700;
  margin-bottom: 1rem;
  color: var(--text-primary);
  animation: fadeInDown 0.6s ease-out;
}

.welcome-subtitle {
  font-size: 1.2rem;
  font-weight: 400;
  margin-bottom: 1rem;
  color: var(--primary-color);
  animation: fadeInDown 0.6s ease-out 0.1s both;
}

.welcome-description {
  font-size: 1rem;
  color: var(--text-secondary);
  line-height: 1.6;
  animation: fadeInDown 0.6s ease-out 0.2s both;
}

.selection-container {
  width: 100%;
  max-width: 1400px;
  animation: fadeInUp 0.6s ease-out 0.3s both;
}

.loading-state,
.error-state,
.empty-state {
  background: var(--card-background);
  padding: 4rem 2rem;
  border-radius: 8px;
  text-align: center;
  box-shadow: 0 2px 8px var(--shadow-color);
}

.spinner {
  width: 50px;
  height: 50px;
  border: 4px solid var(--border-color);
  border-top: 4px solid var(--primary-color);
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error-state {
  color: var(--text-primary);
  
  p {
    color: #e74c3c;
  }
}

.retry-btn {
  margin-top: 1rem;
  padding: 0.75rem 2rem;
  background: var(--primary-color);
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 1rem;
  font-weight: 600;
  transition: all 0.3s;

  &:hover {
    opacity: 0.9;
    transform: translateY(-2px);
    box-shadow: 0 4px 12px var(--shadow-color);
  }
}

.empty-state {
  .empty-icon {
    font-size: 4rem;
    margin-bottom: 1rem;
    opacity: 0.5;
  }

  h2 {
    color: var(--text-primary);
    margin-bottom: 0.5rem;
    font-size: 1.5rem;
  }

  p {
    color: var(--text-secondary);
    font-size: 1rem;
  }
}

.projects-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 2rem;
}

.project-card {
  background: var(--card-background);
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px var(--shadow-color);
  cursor: pointer;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
  border: 2px solid transparent;

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px var(--shadow-color);
    border-color: var(--primary-color);
  }
}

.select-overlay {
  position: absolute;
  top: 0.75rem;
  right: 0.75rem;
  opacity: 0;
  transition: opacity 0.3s;
  pointer-events: none;
  z-index: 10;
}

.project-card:hover .select-overlay {
  opacity: 1;
}

.select-text {
  color: var(--primary-color);
  font-size: 0.875rem;
  font-weight: 500;
  background: var(--card-background);
}

.project-info {
  text-align: center;
  margin-bottom: 1.5rem;
}

.project-name {
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 0.5rem;
}

.language-badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  background: var(--primary-color);
  color: white;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 500;
}

.project-metrics {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.metric {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

.metric-icon {
  font-size: 1.5rem;
}

.metric-content {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.metric-value {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);

  &.excellent { color: #27ae60; }
  &.good { color: #3498db; }
  &.acceptable { color: #f39c12; }
  &.needs-improvement { color: #e74c3c; }
}

.metric-label {
  font-size: 0.75rem;
  color: var(--text-secondary);
  text-transform: uppercase;
}

.project-footer {
  text-align: center;
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
}

.last-analysis {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 768px) {
  .welcome-title {
    font-size: 2.5rem;
  }

  .projects-grid {
    grid-template-columns: 1fr;
  }
}
</style>
