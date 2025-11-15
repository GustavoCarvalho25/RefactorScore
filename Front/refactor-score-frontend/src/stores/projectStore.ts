import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { Project } from '../interfaces/Project';

const STORAGE_KEY = 'refactorscore_selected_project';

export const useProjectStore = defineStore('project', () => {
  const projects = ref<Project[]>([]);
  const selectedProject = ref<string | null>(localStorage.getItem(STORAGE_KEY));
  const loading = ref(false);
  const error = ref<string | null>(null);
  
  const currentProject = computed((): Project | null => {
    if (!selectedProject.value) return null;
    return projects.value.find((p) => p.name === selectedProject.value) || null;
  });
  
  const hasSelectedProject = computed((): boolean => {
    return selectedProject.value !== null && selectedProject.value !== '';
  });
  
  function setProjects(newProjects: Project[]) {
    projects.value = newProjects;
  }
  
  function selectProject(projectName: string | null) {
    selectedProject.value = projectName;
    
    if (projectName) {
      localStorage.setItem(STORAGE_KEY, projectName);
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }
  
  function clearSelection() {
    selectProject(null);
  }
  
  function setLoading(isLoading: boolean) {
    loading.value = isLoading;
  }
  
  function setError(errorMessage: string | null) {
    error.value = errorMessage;
  }
  
  return {
    projects,
    selectedProject,
    loading,
    error,
    currentProject,
    hasSelectedProject,
    setProjects,
    selectProject,
    clearSelection,
    setLoading,
    setError,
  };
});
