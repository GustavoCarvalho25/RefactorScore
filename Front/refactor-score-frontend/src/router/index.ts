import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Home',
    component: () => import('../views/Dashboard.vue'),
    meta: { title: 'Dashboard' }
  },
  {
    path: '/analysis',
    name: 'Analysis',
    component: () => import('../views/AnalysisList.vue').catch(err => {
      console.error('Error loading AnalysisList:', err);
      throw err;
    }),
    meta: { title: 'Análises' }
  },
  {
    path: '/analysis/:id',
    name: 'AnalysisDetail',
    component: () => import('../views/AnalysisDetail.vue'),
    props: true,
    meta: { title: 'Detalhes da Análise' }
  },
  {
    path: '/statistics',
    name: 'Statistics',
    component: () => import('../views/Statistics.vue'),
    meta: { title: 'Estatísticas' }
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
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition;
    } else {
      return { top: 0 };
    }
  }
});

// Tratamento global de erros de navegação
router.onError((error, to) => {
  console.error('Router error:', error);
  console.error('Failed route:', to);
});

// Atualiza o título da página e faz outras preparações
router.beforeEach((to, from) => {
  // Atualiza o título da página
  document.title = `${to.meta.title || 'RefactorScore'} - RefactorScore`;
  
  // Limpa erros anteriores
  console.clear();
  
  return true;
});

export default router;
