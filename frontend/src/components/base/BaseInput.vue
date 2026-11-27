<script setup lang="ts">
import { useFieldControl } from './fieldContext'

/** Single-line text input. Use inside a BaseField for label and error handling. */
withDefaults(
  defineProps<{
    type?: 'text' | 'email' | 'password' | 'tel' | 'url' | 'search' | 'number' | 'date'
  }>(),
  { type: 'text' },
)

const model = defineModel<string>({ default: '' })
const fieldAttrs = useFieldControl()
</script>

<template>
  <input v-model="model" :type="type" class="form-control" v-bind="fieldAttrs" />
</template>

<style scoped src="../../assets/css/form-control.css"></style>
