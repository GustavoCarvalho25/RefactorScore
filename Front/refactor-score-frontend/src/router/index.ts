import { createRouter, createWebHistory, RouteRecordRaw, RouteLocationNormalized } from 'vue-router';
import { useProjectStore } from '../stores/projectStore';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'ProjectSelection',
    component: () => import('../views/ProjectSelection.vue'),
    meta: { title: 'Selecionar Projeto', requiresNoProject: true }
  },
  {
    path: '/dashboard',
    name: 'Home',
    component: () => import('../views/Dashboard.vue'),
    meta: { title: 'Dashboard', requiresProject: true }
  },
  {
    path: '/analysis',
    name: 'Analysis',
    component: () => import('../views/AnalysisList.vue').catch(err => {
      console.error('Error loading AnalysisList:', err);
      throw err;
    }),
    meta: { title: 'Análises', requiresProject: true }
  },
  {
    path: '/analysis/:id',
    name: 'AnalysisDetail',
    component: () => import('../views/AnalysisDetail.vue'),
    props: true,
    meta: { title: 'Detalhes da Análise', requiresProject: true }
  },
  {
    path: '/statistics',
    name: 'Statistics',
    component: () => import('../views/Statistics.vue'),
    meta: { title: 'Estatísticas', requiresProject: true }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('../views/Dashboard.vue'),
    meta: { title: 'Página não encontrada' }
  }
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior(to: RouteLocationNormalized, from: RouteLocationNormalized, savedPosition: any) {
    if (savedPosition) {
      return savedPosition;
    } else {
      return { top: 0 };
    }
  }
});

router.onError((error: Error, to: RouteLocationNormalized) => {
  console.error('Router error:', error);
  console.error('Failed route:', to);
});

router.beforeEach((to: RouteLocationNormalized, from: RouteLocationNormalized) => {
  document.title = `${to.meta.title || 'RefactorScore'} - RefactorScore`;
  
  const projectStore = useProjectStore();
  const hasProject = projectStore.hasSelectedProject;
  
  if (to.meta.requiresProject && !hasProject) {
    return { name: 'ProjectSelection' };
  }
  
  if (to.meta.requiresNoProject && hasProject && from.name !== undefined) {
    return { name: 'Home' };
  }
  
  return true;
});

export default router;
