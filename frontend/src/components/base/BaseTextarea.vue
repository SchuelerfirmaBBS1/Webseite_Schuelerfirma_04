<script setup lang="ts">
import { useFieldControl } from './fieldContext'

/** Multi-line text input. Use inside a BaseField for label and error handling. */
withDefaults(defineProps<{ rows?: number }>(), { rows: 5 })

const model = defineModel<string>({ default: '' })
const fieldAttrs = useFieldControl()
</script>

<template>
  <textarea v-model="model" :rows="rows" class="form-control" v-bind="fieldAttrs"></textarea>
</template>

<style scoped src="../../assets/css/form-control.css"></style>

<style scoped>
.form-control {
  min-height: 140px;
  resize: vertical;
}
</style>
