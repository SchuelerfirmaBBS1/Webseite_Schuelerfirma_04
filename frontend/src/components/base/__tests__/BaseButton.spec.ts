import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import BaseButton from '../BaseButton.vue'

const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: '/', component: { render: () => null } },
    { path: '/termine', component: { render: () => null } },
  ],
})

function mountButton(props: Record<string, unknown> = {}, attrs: Record<string, unknown> = {}) {
  return mount(BaseButton, {
    props,
    attrs,
    slots: { default: 'Klick' },
    global: { plugins: [router] },
  })
}

describe('BaseButton', () => {
  it('renders a <button type="button"> by default with primary variant', () => {
    const wrapper = mountButton()
    const button = wrapper.get('button')
    expect(button.attributes('type')).toBe('button')
    expect(button.classes()).toContain('base-button--primary')
    expect(button.text()).toBe('Klick')
  })

  it('renders a router link when "to" is set', () => {
    const wrapper = mountButton({ to: '/termine', variant: 'secondary' })
    const link = wrapper.get('a')
    expect(link.attributes('href')).toBe('/termine')
    expect(link.classes()).toContain('base-button--secondary')
  })

  it('renders a plain link when "href" is set', () => {
    const wrapper = mountButton({ href: '#kontakt' })
    expect(wrapper.get('a').attributes('href')).toBe('#kontakt')
  })

  it('falls back to a disabled button when a link is disabled, so it cannot be followed', () => {
    const wrapper = mountButton({ to: '/termine', disabled: true })
    expect(wrapper.find('a').exists()).toBe(false)
    expect(wrapper.get('button').attributes('disabled')).toBeDefined()
  })

  it('does not fire clicks while disabled', async () => {
    const onClick = vi.fn()
    const wrapper = mountButton({ disabled: true }, { onClick })
    await wrapper.get('button').trigger('click')
    expect(onClick).not.toHaveBeenCalled()
  })

  it('fires clicks when enabled', async () => {
    const onClick = vi.fn()
    const wrapper = mountButton({}, { onClick })
    await wrapper.get('button').trigger('click')
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('marks the button busy and disabled while loading', () => {
    const wrapper = mountButton({ loading: true, arrow: true })
    const button = wrapper.get('button')
    expect(button.attributes('aria-busy')).toBe('true')
    expect(button.attributes('disabled')).toBeDefined()
    expect(wrapper.find('.base-button__arrow').exists()).toBe(false)
  })

  it('uses the given native type, e.g. for submit buttons', () => {
    const wrapper = mountButton({ type: 'submit' })
    expect(wrapper.get('button').attributes('type')).toBe('submit')
  })
})
