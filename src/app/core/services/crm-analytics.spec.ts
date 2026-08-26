import {
  closingSoon,
  contractsByArea,
  contractsByMonth,
  countBy,
  isContractOpen,
  isOportunityOpen,
  isOportunityWon,
  newestContracts,
  overdueOportunities,
  pipelineValue,
  weightedPipeline,
} from './crm-analytics';
import { ContractGrid, OportunityResponse } from '../../shared/models/api.model';

function contract(partial: Partial<ContractGrid>): ContractGrid {
  return {
    contractId: 1,
    referenceNumber: 'BNC-1',
    clientName: 'Cliente',
    productName: 'Producto',
    areaName: 'Banca',
    contractStatusName: 'Vigente',
    dateOpeningIssue: new Date().toISOString(),
    ...partial,
  };
}

function oportunity(partial: Partial<OportunityResponse>): OportunityResponse {
  return {
    oportunityId: 1,
    clientId: 1,
    clientName: 'Cliente',
    prdoductName: 'Producto',
    areaName: 'Banca',
    assignedUserName: 'Asesor',
    estimatedMont: 100000,
    stageName: 'Prospección',
    succesProbability: 50,
    dateEstimatedClose: null,
    dateRegister: new Date().toISOString(),
    ...partial,
  };
}

function monthsAgo(n: number): string {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth() - n, 15, 12).toISOString();
}
const daysAhead = (n: number) => new Date(Date.now() + n * 86_400_000).toISOString();
const daysAgo = (n: number) => new Date(Date.now() - n * 86_400_000).toISOString();

describe('crm analytics', () => {
  describe('isContractOpen', () => {
    it('cuenta como vigente lo que no está cerrado', () => {
      expect(isContractOpen(contract({ contractStatusName: 'Vigente' }))).toBe(true);
      expect(isContractOpen(contract({ contractStatusName: 'En trámite' }))).toBe(true);
    });

    it('excluye los estados terminales', () => {
      for (const estado of ['Cancelado', 'Vencido', 'Liquidado', 'Rechazado']) {
        expect(isContractOpen(contract({ contractStatusName: estado })))
          .withContext(estado)
          .toBe(false);
      }
    });

    it('no revienta con un estado ausente', () => {
      expect(isContractOpen(contract({ contractStatusName: null as unknown as string }))).toBe(true);
    });
  });

  describe('contractsByArea', () => {
    it('separa Banca de Seguros', () => {
      const serie = contractsByArea(
        [
          contract({ contractId: 1, areaName: 'Banca', dateOpeningIssue: monthsAgo(0) }),
          contract({ contractId: 2, areaName: 'Banca', dateOpeningIssue: monthsAgo(0) }),
          contract({ contractId: 3, areaName: 'Seguros', dateOpeningIssue: monthsAgo(0) }),
        ],
        6,
      );
      const actual = serie[serie.length - 1];
      expect(actual.primary).toBe(2);
      expect(actual.secondary).toBe(1);
    });

    it('NO mete el área General en el cubo de Seguros', () => {
      // El catálogo tiene tres áreas; la leyenda de la gráfica solo nombra dos.
      const serie = contractsByArea(
        [contract({ areaName: 'General', dateOpeningIssue: monthsAgo(0) })],
        6,
      );
      expect(serie.every((p) => p.primary === 0 && p.secondary === 0)).toBe(true);
    });

    it('mantiene los meses vacíos en el eje', () => {
      const serie = contractsByArea([contract({ dateOpeningIssue: monthsAgo(0) })], 6);
      expect(serie.length).toBe(6);
      expect(serie.filter((p) => p.primary === 0 && p.secondary === 0).length).toBe(5);
    });
  });

  describe('contractsByMonth', () => {
    it('es acumulado y nunca decrece', () => {
      const serie = contractsByMonth(
        [
          contract({ contractId: 1, dateOpeningIssue: monthsAgo(5) }),
          contract({ contractId: 2, dateOpeningIssue: monthsAgo(3) }),
          contract({ contractId: 3, dateOpeningIssue: monthsAgo(1) }),
        ],
        6,
      );
      for (let i = 1; i < serie.length; i++) {
        expect(serie[i].value).toBeGreaterThanOrEqual(serie[i - 1].value);
      }
      expect(serie[serie.length - 1].value).toBe(3);
    });

    it('devuelve vacío si ninguna fecha sirve', () => {
      expect(contractsByMonth([contract({ dateOpeningIssue: 'no es fecha' })], 6)).toEqual([]);
      expect(contractsByMonth([], 6)).toEqual([]);
    });
  });

  describe('embudo', () => {
    it('distingue abiertas, ganadas y perdidas', () => {
      expect(isOportunityOpen(oportunity({ stageName: 'Negociación' }))).toBe(true);
      expect(isOportunityOpen(oportunity({ stageName: 'Ganada' }))).toBe(false);
      expect(isOportunityOpen(oportunity({ stageName: 'Perdida' }))).toBe(false);
      expect(isOportunityWon(oportunity({ stageName: 'Ganada' }))).toBe(true);
      expect(isOportunityWon(oportunity({ stageName: 'Perdida' }))).toBe(false);
    });

    it('sólo suma las oportunidades vivas', () => {
      const rows = [
        oportunity({ oportunityId: 1, estimatedMont: 100000, stageName: 'Cotización' }),
        oportunity({ oportunityId: 2, estimatedMont: 900000, stageName: 'Ganada' }),
      ];
      expect(pipelineValue(rows)).toBe(100000);
    });

    it('pondera por probabilidad', () => {
      const rows = [
        oportunity({ oportunityId: 1, estimatedMont: 100000, succesProbability: 25 }),
        oportunity({ oportunityId: 2, estimatedMont: 200000, succesProbability: 50 }),
      ];
      expect(weightedPipeline(rows)).toBe(125000);
    });

    it('trata un monto nulo como cero, no como NaN', () => {
      const rows = [oportunity({ estimatedMont: null })];
      expect(pipelineValue(rows)).toBe(0);
      expect(weightedPipeline(rows)).toBe(0);
    });

    it('sólo marca vencida una oportunidad viva con fecha pasada', () => {
      const rows = [
        oportunity({ oportunityId: 1, dateEstimatedClose: daysAgo(5) }),
        oportunity({ oportunityId: 2, dateEstimatedClose: daysAhead(5) }),
        oportunity({ oportunityId: 3, dateEstimatedClose: daysAgo(5), stageName: 'Ganada' }),
        oportunity({ oportunityId: 4, dateEstimatedClose: null }),
      ];
      expect(overdueOportunities(rows).map((o) => o.oportunityId)).toEqual([1]);
    });

    it('ordena los cierres próximos por fecha y omite los que no la tienen', () => {
      const rows = [
        oportunity({ oportunityId: 1, dateEstimatedClose: daysAhead(30) }),
        oportunity({ oportunityId: 2, dateEstimatedClose: daysAhead(3) }),
        oportunity({ oportunityId: 3, dateEstimatedClose: null }),
      ];
      expect(closingSoon(rows, 5).map((o) => o.oportunityId)).toEqual([2, 1]);
    });
  });

  describe('countBy', () => {
    it('ordena por tamaño y luego alfabéticamente', () => {
      const rows = countBy(
        [
          contract({ contractStatusName: 'Vigente' }),
          contract({ contractStatusName: 'Vigente' }),
          contract({ contractStatusName: 'Cancelado' }),
          contract({ contractStatusName: 'Activo' }),
        ],
        (c) => c.contractStatusName,
        'Sin estado',
      );
      expect(rows.map((r) => r.name)).toEqual(['Vigente', 'Activo', 'Cancelado']);
    });

    it('agrupa los vacíos bajo una etiqueta', () => {
      const rows = countBy(
        [contract({ areaName: '' }), contract({ areaName: '   ' })],
        (c) => c.areaName,
        'Sin área',
      );
      expect(rows).toEqual([{ name: 'Sin área', value: 2 }]);
    });
  });

  it('newestContracts no muta el arreglo recibido', () => {
    const input = [
      contract({ contractId: 1, dateOpeningIssue: monthsAgo(5) }),
      contract({ contractId: 2, dateOpeningIssue: monthsAgo(1) }),
    ];
    expect(newestContracts(input, 2).map((c) => c.contractId)).toEqual([2, 1]);
    expect(input.map((c) => c.contractId)).toEqual([1, 2]);
  });
});
