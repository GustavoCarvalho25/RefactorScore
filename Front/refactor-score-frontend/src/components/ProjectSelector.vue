<template>
  <div class="project-selector">
    <div class="selector-header">
      <h2>Selecione um Projeto</h2>
      <button v-if="hasSelectedProject" @click="clearProject" class="clear-btn">
        Limpar Seleção
      </button>
    </div>
    
    <div v-if="loading" class="loading-state">
      <p>Carregando projetos...</p>
    </div>
    
    <div v-else-if="error" class="error-state">
      <p>Erro ao carregar projetos: {{ error }}</p>
    </div>
    
    <div v-else-if="projects.length === 0" class="empty-state">
      <p>Nenhum projeto encontrado. Execute análises para visualizar projetos.</p>
    </div>
    
    <div v-else class="projects-container">
      <div 
        v-for="project in projects" 
        :key="project.name"
        :class="['project-card', { selected: isSelected(project.name) }]"
        @click="selectProject(project.name)"
      >
        <div class="project-header">
          <h3>{{ project.name }}</h3>
          <span class="language-badge">{{ project.mainLanguage }}</span>
        </div>
        
        <div class="project-stats">
          <div class="stat">
            <span class="stat-label">Commits</span>
            <span class="stat-value">{{ project.totalCommits }}</span>
          </div>
          <div class="stat">
            <span class="stat-label">Nota Média</span>
            <span :class="['stat-value', getScoreClass(project.averageNote)]">
              {{ project.averageNote.toFixed(1) }}
            </span>
          </div>
          <div class="stat">
            <span class="stat-label">Arquivos</span>
            <span class="stat-value">{{ project.totalFiles }}</span>
          </div>
        </div>
        
        <div class="project-footer">
          <span class="last-analysis">
            Última análise: {{ formatDate(project.lastAnalysisDate) }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { storeToRefs } from 'pinia';
import { useProjectStore } from '../stores/projectStore';
import { useProjectService } from '../server/api/projectService';
import type { Project } from '../interfaces/Project';

const projectStore = useProjectStore();
const { selectedProject, hasSelectedProject } = storeToRefs(projectStore);
const projectService = useProjectService();

const projects = ref<Project[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

const isSelected = (projectName: string): boolean => {
  return selectedProject.value === projectName;
};

const selectProject = (projectName: string) => {
  projectStore.selectProject(projectName);
  // Emitir evento para atualizar dashboard
  window.dispatchEvent(new CustomEvent('project-changed', { detail: projectName }));
};

const clearProject = () => {
  projectStore.clearSelection();
  window.dispatchEvent(new CustomEvent('project-changed', { detail: null }));
};

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

<style lang="scss" scoped>
.project-selector {
  margin-bottom: 2rem;
}

.selector-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.selector-header h2 {
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--text-primary, #1a1a1a);
}

.clear-btn {
  padding: 0.5rem 1rem;
  background: var(--danger-color, #dc3545);
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
  transition: background 0.2s;
}

.clear-btn:hover {
  background: var(--danger-hover, #c82333);
}

.loading-state,
.error-state,
.empty-state {
  text-align: center;
  padding: 2rem;
  background: var(--card-bg, #f8f9fa);
  border-radius: 8px;
}

.error-state {
  background: var(--error-bg, #fee);
  color: var(--error-color, #c00);
}

.projects-container {
  display: flex;
  gap: 1rem;
  overflow-x: auto;
  padding-bottom: 1rem;
  scroll-behavior: smooth;
}

.projects-container::-webkit-scrollbar {
  height: 8px;
}

.projects-container::-webkit-scrollbar-track {
  background: var(--scrollbar-track, #f1f1f1);
  border-radius: 4px;
}

.projects-container::-webkit-scrollbar-thumb {
  background: var(--scrollbar-thumb, #888);
  border-radius: 4px;
}

.projects-container::-webkit-scrollbar-thumb:hover {
  background: var(--scrollbar-thumb-hover, #555);
}

.project-card {
  min-width: 280px;
  max-width: 320px;
  padding: 1.5rem;
  background: var(--card-bg, white);
  border: 2px solid var(--border-color, #e0e0e0);
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
  flex-shrink: 0;
}

.project-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
  border-color: var(--primary-color, #007bff);
}

.project-card.selected {
  border-color: var(--primary-color, #007bff);
  background: var(--primary-light, #e7f3ff);
  box-shadow: 0 4px 12px rgba(0, 123, 255, 0.2);
}

.project-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.project-header h3 {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--text-primary, #1a1a1a);
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.language-badge {
  padding: 0.25rem 0.75rem;
  background: var(--badge-bg, #6c757d);
  color: white;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 500;
}

.project-stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  margin-bottom: 1rem;
}

.stat {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.stat-label {
  font-size: 0.75rem;
  color: var(--text-secondary, #666);
  margin-bottom: 0.25rem;
}

.stat-value {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary, #1a1a1a);
}

.stat-value.excellent {
  color: var(--success-color, #28a745);
}

.stat-value.good {
  color: var(--info-color, #17a2b8);
}

.stat-value.acceptable {
  color: var(--warning-color, #ffc107);
}

.stat-value.needs-improvement {
  color: var(--danger-color, #dc3545);
}

.project-footer {
  padding-top: 1rem;
  border-top: 1px solid var(--border-color, #e0e0e0);
}

.last-analysis {
  font-size: 0.875rem;
  color: var(--text-secondary, #666);
}

@media (max-width: 768px) {
  .project-card {
    min-width: 240px;
  }
  
  .selector-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }
}
</style>
