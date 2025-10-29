<script setup lang="ts">
import { RouterView } from 'vue-router';
import { useTheme } from './composables/useTheme';
import { onMounted } from 'vue';

const { isDark, toggleTheme, initTheme } = useTheme();

onMounted(() => {
  initTheme();
});
</script>

<template>
  <div id="app">
    <nav class="navbar">
      <div class="navbar-brand">
        <router-link to="/" class="brand-link">
          <h1>RefactorScore</h1>
          <span class="subtitle">Clean Code Analysis</span>
        </router-link>
      </div>
      <div class="navbar-menu">
        <router-link to="/" class="nav-link">Dashboard</router-link>
        <router-link to="/analysis" class="nav-link">Análises</router-link>
        <router-link to="/statistics" class="nav-link">Estatísticas</router-link>
        <button class="theme-toggle" @click="toggleTheme" :title="isDark ? 'Modo Claro' : 'Modo Escuro'">
          <span v-if="isDark">☀️</span>
          <span v-else>🌙</span>
        </button>
      </div>
    </nav>

    <main class="main-content">
      <RouterView />
    </main>

    <footer class="footer">
      <p>&copy; 2025 RefactorScore - TCC Project</p>
    </footer>
  </div>
</template>

<style lang="scss">
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen,
    Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
  background: var(--background-color);
  color: var(--text-primary);
  transition: background-color 0.3s ease, color 0.3s ease;
}

#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.navbar {
  background: var(--nav-background);
  box-shadow: 0 2px 8px var(--shadow-color);
  padding: 1rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;

  .navbar-brand {
    .brand-link {
      text-decoration: none;
      display: flex;
      flex-direction: column;

      h1 {
        font-size: 1.5rem;
        color: #447bda;
        font-weight: bold;
      }

      .subtitle {
        font-size: 0.75rem;
        color: #7f8c8d;
        text-transform: uppercase;
        letter-spacing: 1px;
      }
    }
  }

  .navbar-menu {
    display: flex;
    gap: 2rem;

    .nav-link {
      text-decoration: none;
      color: var(--text-primary);
      font-weight: 500;
      padding: 0.5rem 1rem;
      border-radius: 6px;
      transition: all 0.3s ease;

      &:hover {
        background: var(--background-color);
        color: #447bda;
      }

      &.router-link-active {
        background: #447bda;
        color: white;
      }
    }

    .theme-toggle {
      background: var(--nav-background);
      border: 2px solid var(--border-color);
      border-radius: 50%;
      width: 35px;
      height: 35px;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      font-size: 1.2rem;
      transition: all 0.3s ease;
      margin-left: 1rem;

      &:hover {
        transform: scale(1.1);
        border-color: #447bda;
      }

      &:active {
        transform: scale(0.95);
      }
    }
  }
}

.main-content {
  flex: 1;
  width: 100%;
}

.footer {
  background: var(--nav-background);
  padding: 1.5rem 2rem;
  text-align: center;
  border-top: 1px solid var(--border-color);
  margin-top: 2rem;
  transition: background-color 0.3s ease, border-color 0.3s ease;

  p {
    color: var(--text-secondary);
    font-size: 0.9rem;
  }
}
</style>
