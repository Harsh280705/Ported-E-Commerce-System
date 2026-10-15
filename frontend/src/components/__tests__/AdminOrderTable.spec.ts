import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AdminOrderTable from '@/components/AdminOrderTable.vue'

describe('AdminOrderTable', () => {
  it('renders rows and emits status updates', async () => {
    const wrapper = mount(AdminOrderTable, {
      props: {
        orders: [
          { id: 'o1', orderNumber: 'ORD-1', orderDate: new Date().toISOString(), status: 'Pending', total: 500, itemCount: 2 }
        ]
      }
    })
    expect(wrapper.text()).toContain('ORD-1')
    const select = wrapper.find('select')
    await select.setValue('Shipped')
    expect(wrapper.emitted('update-status')).toBeTruthy()
    expect(wrapper.emitted('update-status')![0][0]).toEqual({ id: 'o1', status: 'Shipped' })
  })
})
