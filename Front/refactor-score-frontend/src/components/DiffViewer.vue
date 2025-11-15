<template>
  <div class="diff-container">
    <div class="diff-content">
      <pre>
        <code>
          <div v-for="(line, index) in parsedDiff" :key="index" :class="['diff-line', line.type]">
            <span class="line-prefix">{{ line.prefix }}</span>
            <span class="line-content">{{ line.content }}</span>
          </div>
        </code>
      </pre>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useTheme } from '../composables/useTheme';

const { isDark } = useTheme();

const props = defineProps<{
  content: string;
}>();

const parsedDiff = computed(() => {
  const lines = props.content.split('\n');
  return lines.map(line => {
    if (line.startsWith('+')) {
      return { type: 'addition', prefix: '+', content: line.substring(1) };
    } else if (line.startsWith('-')) {
      return { type: 'deletion', prefix: '-', content: line.substring(1) };
    } else if (line.startsWith('@@')) {
      return { type: 'info', prefix: '', content: line };
    } else {
      return { type: 'context', prefix: ' ', content: line };
    }
  });
});
</script>

<style scoped lang="scss">
.diff-container {
  border: 1px solid var(--border-color);
  border-radius: 6px;
  margin: 10px 0;
  max-height: 400px;
  overflow-y: auto;
}

.diff-content {
  background-color: v-bind("isDark ? '#1a1a2e' : '#f8f9fa'");
  font-family: monospace;
  font-size: 12px;
  line-height: 1.5;
  white-space: pre;
}

.diff-line {
  display: flex;
  padding: 0 10px;

  &.addition {
    background-color: v-bind("isDark ? '#1a331f' : '#e6ffec'");
    color: v-bind("isDark ? '#7ee787' : '#24292e'");
  }

  &.deletion {
    background-color: v-bind("isDark ? '#331a1d' : '#ffebe9'");
    color: v-bind("isDark ? '#ff7b72' : '#24292e'");
  }

  &.info {
    color: v-bind("isDark ? '#8b949e' : '#6e7781'");
    background-color: v-bind("isDark ? '#161b22' : '#f6f8fa'");
  }

  &.context {
    color: v-bind("isDark ? '#c9d1d9' : '#24292e'");
  }
}

.line-prefix {
  width: 20px;
  user-select: none;
}

.line-content {
  flex: 1;
}
</style>