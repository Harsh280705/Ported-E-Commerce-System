import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import Pagination from '@/components/Pagination.vue'

describe('Pagination', () => {
  it('renders page buttons and emits change', async () => {
    const wrapper = mount(Pagination, { props: { page: 2, totalPages: 5 } })
    expect(wrapper.text()).toContain('2')
    const next = wrapper.find('button[aria-label="Next page"]')
    await next.trigger('click')
    expect(wrapper.emitted('change')).toBeTruthy()
    expect(wrapper.emitted('change')![0]).toEqual([3])
  })

  it('disables prev on first page', () => {
    const wrapper = mount(Pagination, { props: { page: 1, totalPages: 3 } })
    const prev = wrapper.find('button[aria-label="Previous page"]')
    expect((prev.element as HTMLButtonElement).disabled).toBe(true)
  })

  it('disables next on last page', () => {
    const wrapper = mount(Pagination, { props: { page: 3, totalPages: 3 } })
    const next = wrapper.find('button[aria-label="Next page"]')
    expect((next.element as HTMLButtonElement).disabled).toBe(true)
  })
})
