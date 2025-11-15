import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import './assets/styles/main.scss';

const app = createApp(App);
const pinia = createPinia();

// Configuração global de erro
app.config.errorHandler = (err, instance, info) => {
  console.error('Vue Global Error:', err);
  console.error('Error Info:', info);
  if (instance) {
    console.error('Component:', instance.$options.name);
  }
};

// Handler de promessas rejeitadas não tratadas
window.addEventListener('unhandledrejection', event => {
  console.error('Unhandled Promise Rejection:', event.reason);
});

// Handler de erros não tratados
window.addEventListener('error', event => {
  console.error('Global Error:', event.error);
});

app.use(pinia);
app.use(router);

// Inicia a aplicação quando o router estiver pronto
router.isReady().then(() => {
  app.mount('#app');
}).catch(err => {
  console.error('Failed to initialize app:', err);
});
