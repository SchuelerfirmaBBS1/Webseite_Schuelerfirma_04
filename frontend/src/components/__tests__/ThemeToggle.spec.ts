import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ThemeToggle from '../ThemeToggle.vue'

describe('ThemeToggle', () => {
  it('starts in light mode and offers the dark mode (half moon)', () => {
    const button = mount(ThemeToggle).get('button')
    expect(button.attributes('aria-pressed')).toBe('false')
    expect(button.attributes('aria-label')).toBe('Dunklen Modus aktivieren')
    expect(button.find('circle').exists()).toBe(false)
  })

  it('swaps to the sun on click and back again', async () => {
    const wrapper = mount(ThemeToggle)
    const button = wrapper.get('button')

    await button.trigger('click')
    expect(button.attributes('aria-pressed')).toBe('true')
    expect(button.attributes('aria-label')).toBe('Hellen Modus aktivieren')
    expect(button.find('circle').exists()).toBe(true)

    await button.trigger('click')
    expect(button.attributes('aria-pressed')).toBe('false')
    expect(button.find('circle').exists()).toBe(false)
  })
})
