import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseFrame from '../BaseFrame.vue'

describe('BaseFrame', () => {
  it('defaults to an arch on a blue plate, tilted left, without label', () => {
    const wrapper = mount(BaseFrame, { props: { src: '/a.png', alt: 'Anhänger' } })
    const classes = wrapper.get('figure').classes()
    expect(classes).toEqual(expect.arrayContaining(['base-frame--arch', 'base-frame--plate-blue']))
    // Left is the default tilt in CSS, so only "right" needs a modifier class
    expect(classes).not.toContain('base-frame--tilt-right')
    expect(wrapper.find('figcaption').exists()).toBe(false)
  })

  it('renders the image with its alt text', () => {
    const wrapper = mount(BaseFrame, { props: { src: '/a.png', alt: 'Anhänger' } })
    expect(wrapper.get('img').attributes()).toMatchObject({ src: '/a.png', alt: 'Anhänger' })
  })

  it('hides the plate from assistive technology', () => {
    const wrapper = mount(BaseFrame, { props: { src: '/a.png', alt: '' } })
    expect(wrapper.get('.base-frame__plate').attributes('aria-hidden')).toBe('true')
  })

  it('applies shape, plate, tilt and label options', () => {
    const wrapper = mount(BaseFrame, {
      props: { src: '/a.png', alt: '', shape: 'round', plate: 'ink', tilt: 'right', label: 'Bite', labelTone: 'terracotta' },
    })
    expect(wrapper.get('figure').classes()).toEqual(
      expect.arrayContaining(['base-frame--round', 'base-frame--plate-ink', 'base-frame--tilt-right']),
    )
    const label = wrapper.get('figcaption')
    expect(label.text()).toBe('Bite')
    expect(label.classes()).toContain('base-frame__label--terracotta')
  })
})
