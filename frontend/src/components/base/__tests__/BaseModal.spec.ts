import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseModal from '../BaseModal.vue'

// jsdom does not implement showModal/close of <dialog>; emulate the parts we rely on
const dialogProto = HTMLDialogElement.prototype as Partial<HTMLDialogElement>

beforeEach(() => {
  dialogProto.showModal = vi.fn(function (this: HTMLDialogElement) {
    this.setAttribute('open', '')
  })
  dialogProto.close = vi.fn(function (this: HTMLDialogElement) {
    this.removeAttribute('open')
    this.dispatchEvent(new Event('close'))
  })
})

afterEach(() => {
  delete dialogProto.showModal
  delete dialogProto.close
})

function mountModal(open: boolean) {
  return mount(BaseModal, {
    props: { title: 'Quizfrage', open },
    slots: { default: '<p class="content">Inhalt</p>' },
    attachTo: document.body,
  })
}

describe('BaseModal', () => {
  it('opens the dialog as modal when open is true on mount', () => {
    const wrapper = mountModal(true)
    expect(HTMLDialogElement.prototype.showModal).toHaveBeenCalledOnce()
    expect(wrapper.get('dialog').attributes('open')).toBeDefined()
    wrapper.unmount()
  })

  it('stays closed when open is false', () => {
    const wrapper = mountModal(false)
    expect(HTMLDialogElement.prototype.showModal).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('opens and closes when the open prop changes', async () => {
    const wrapper = mountModal(false)
    await wrapper.setProps({ open: true })
    expect(HTMLDialogElement.prototype.showModal).toHaveBeenCalledOnce()
    await wrapper.setProps({ open: false })
    expect(HTMLDialogElement.prototype.close).toHaveBeenCalledOnce()
    wrapper.unmount()
  })

  it('labels the dialog with its title', () => {
    const wrapper = mountModal(true)
    const labelledBy = wrapper.get('dialog').attributes('aria-labelledby')
    expect(wrapper.get(`#${CSS.escape(labelledBy!)}`).text()).toBe('Quizfrage')
    wrapper.unmount()
  })

  it('reports closing when the browser closes it (Esc key)', async () => {
    const wrapper = mountModal(true)
    await wrapper.get('dialog').trigger('close')
    expect(wrapper.emitted('update:open')).toEqual([[false]])
    wrapper.unmount()
  })

  it('closes on a click on the backdrop but not on the content', async () => {
    const wrapper = mountModal(true)
    await wrapper.get('.content').trigger('click')
    expect(wrapper.emitted('update:open')).toBeUndefined()

    await wrapper.get('dialog').trigger('click')
    expect(wrapper.emitted('update:open')).toEqual([[false]])
    wrapper.unmount()
  })

  it('closes with the close button', async () => {
    const wrapper = mountModal(true)
    await wrapper.get('button[aria-label="Schließen"]').trigger('click')
    expect(wrapper.emitted('update:open')).toEqual([[false]])
    wrapper.unmount()
  })
})
