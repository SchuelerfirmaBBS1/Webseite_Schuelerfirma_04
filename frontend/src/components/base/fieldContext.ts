import { computed, inject, type ComputedRef, type InjectionKey } from 'vue'

/** Wiring a BaseField hands down to the control inside it. */
export interface FieldContext {
  id: string
  describedBy: ComputedRef<string | undefined>
  invalid: ComputedRef<boolean>
  required: ComputedRef<boolean>
}

export const fieldContextKey: InjectionKey<FieldContext> = Symbol('BaseField')

/** Attributes for a native control, so it is linked to the label, hint and error of its BaseField. */
export function useFieldControl() {
  const field = inject(fieldContextKey, null)

  return computed(() =>
    field
      ? {
          id: field.id,
          'aria-describedby': field.describedBy.value,
          'aria-invalid': field.invalid.value || undefined,
          required: field.required.value || undefined,
        }
      : {},
  )
}
