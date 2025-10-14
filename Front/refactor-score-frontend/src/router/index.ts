import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Home',
    component: () => import('../views/Dashboard.vue'),
  },
  {
    path: '/analysis',
    name: 'Analysis',
    component: () => import('../views/AnalysisList.vue'),
  },
  {
    path: '/analysis/:id',
    name: 'AnalysisDetail',
    component: () => import('../views/AnalysisDetail.vue'),
    props: true,
  },
  {
    path: '/statistics',
    name: 'Statistics',
    component: () => import('../views/Statistics.vue'),
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

export default router;
