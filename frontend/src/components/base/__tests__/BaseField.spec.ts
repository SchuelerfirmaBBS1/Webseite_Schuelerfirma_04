import { describe, expect, it } from 'vitest'
import { defineComponent, h, ref, type Component } from 'vue'
import { mount } from '@vue/test-utils'
import BaseField from '../BaseField.vue'
import BaseInput from '../BaseInput.vue'
import BaseTextarea from '../BaseTextarea.vue'
import BaseSelect from '../BaseSelect.vue'
import BaseCheckbox from '../BaseCheckbox.vue'

function mountField(
  fieldProps: Record<string, unknown>,
  control: Component = BaseInput,
  controlProps: Record<string, unknown> = {},
) {
  return mount(BaseField, {
    props: { label: 'E-Mail', ...fieldProps },
    slots: { default: () => h(control, controlProps) },
  })
}

describe('BaseField with controls', () => {
  it('links the label to the control', () => {
    const wrapper = mountField({})
    const id = wrapper.get('input').attributes('id')
    expect(id).toBeTruthy()
    expect(wrapper.get('label').attributes('for')).toBe(id)
  })

  it('has no aria-describedby or aria-invalid without hint and error', () => {
    const input = mountField({}).get('input')
    expect(input.attributes('aria-describedby')).toBeUndefined()
    expect(input.attributes('aria-invalid')).toBeUndefined()
  })

  it('wires hint and error to the control', () => {
    const wrapper = mountField({ hint: 'Wir antworten per Mail.', error: 'Ungültige Adresse.' })
    const input = wrapper.get('input')
    const describedBy = input.attributes('aria-describedby')?.split(' ') ?? []

    expect(describedBy).toHaveLength(2)
    for (const id of describedBy) {
      expect(wrapper.find(`#${CSS.escape(id)}`).exists()).toBe(true)
    }
    expect(input.attributes('aria-invalid')).toBe('true')
    expect(wrapper.get('[role="alert"]').text()).toBe('Ungültige Adresse.')
  })

  it('passes "required" on to the control', () => {
    const wrapper = mountField({ required: true })
    expect(wrapper.get('input').attributes('required')).toBeDefined()
  })

  it('wires textarea and select the same way', () => {
    const textarea = mountField({ error: 'Fehlt.' }, BaseTextarea)
    expect(textarea.get('textarea').attributes('aria-invalid')).toBe('true')

    const select = mountField({}, BaseSelect, { options: [{ value: 'a', label: 'A' }] })
    expect(select.get('label').attributes('for')).toBe(select.get('select').attributes('id'))
  })
})

describe('form controls v-model', () => {
  it('BaseInput updates its model', async () => {
    const value = ref('')
    const Host = defineComponent({
      setup: () => () =>
        h(BaseInput, { modelValue: value.value, 'onUpdate:modelValue': (v: string) => (value.value = v) }),
    })
    const wrapper = mount(Host)
    await wrapper.get('input').setValue('hallo@example.org')
    expect(value.value).toBe('hallo@example.org')
  })

  it('BaseSelect renders the placeholder as a disabled first option', () => {
    const wrapper = mount(BaseSelect, {
      props: { options: [{ value: 'a', label: 'A' }], placeholder: 'Bitte wählen' },
    })
    const options = wrapper.findAll('option')
    expect(options).toHaveLength(2)
    expect(options[0]!.attributes('disabled')).toBeDefined()
  })

  it('BaseCheckbox toggles its model and links its label', async () => {
    const wrapper = mount(BaseCheckbox, {
      props: { modelValue: false, 'onUpdate:modelValue': (v: boolean) => wrapper.setProps({ modelValue: v }) },
      slots: { default: 'Einverstanden' },
    })
    const input = wrapper.get('input')
    expect(wrapper.get('label').attributes('for')).toBe(input.attributes('id'))
    await input.setValue(true)
    expect(wrapper.props('modelValue')).toBe(true)
  })
})
