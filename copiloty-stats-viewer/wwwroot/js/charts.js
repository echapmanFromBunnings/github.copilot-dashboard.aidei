window.copilotCharts = (function(){
  const charts = {};

  function ensureChart(id, type, data, options){
    const canvas = document.getElementById(id);
    if (!canvas) {
      console.warn(`Canvas element with id '${id}' not found`);
      return;
    }
    const ctx = canvas.getContext('2d');
    if(charts[id]){
      charts[id].data = data;
      charts[id].options = options || {};
      charts[id].update();
      return;
    }
    charts[id] = new Chart(ctx, { type, data, options: options || {} });
  }

  return {
    timeSeries: function(id, labels, interactions, generations, acceptances){
      const data = {
        labels,
        datasets: [
      { label: 'Interactions', data: interactions, borderColor: '#2b6c70', backgroundColor: 'rgba(43,108,112,0.18)', pointRadius: 0, tension: 0.25 },
      { label: 'Generations', data: generations, borderColor: '#005358', backgroundColor: 'rgba(0,83,88,0.2)', pointRadius: 0, tension: 0.25 },
      { label: 'Acceptances', data: acceptances, borderColor: '#7da8ab', backgroundColor: 'rgba(125,168,171,0.25)', pointRadius: 0, tension: 0.25 },
        ]
      };
    ensureChart(id, 'line', data, { responsive: true, interaction: { mode: 'index', intersect: false }, plugins: { legend: { position: 'bottom' }}, scales: { x: { grid: { display: false } }, y: { grid: { color: 'rgba(0,0,0,0.05)' } } } });
    },
    barMix: function(id, labels, counts, label){
    const data = { labels, datasets: [{ label, data: counts, backgroundColor: 'rgba(0,83,88,0.65)', borderColor: '#005358', borderWidth: 1, borderRadius: 4 }] };
    ensureChart(id, 'bar', data, { indexAxis: 'y', responsive: true, plugins: { legend: { display: false }}, scales: { x: { grid: { color: 'rgba(0,0,0,0.05)' } }, y: { grid: { display: false } } } });
    },
    pieMix: function(id, labels, counts){
    const base = ['#005358','#2b6c70','#4b8588','#7da8ab','#cfe2e3','#0b7d77','#3d8b8e','#9cc7c9','#1f6b6f','#74a4a6'];
    const colors = labels.map((_, i) => base[i % base.length]);
      const data = { labels, datasets: [{ data: counts, backgroundColor: colors }] };
    ensureChart(id, 'doughnut', data, { responsive: true, plugins: { legend: { position: 'right' }}, cutout: '60%' });
    },
    lineChart: function(id, labels, data, label, color){
      const chartData = {
        labels,
        datasets: [{
          label: label,
          data: data,
          borderColor: color,
          backgroundColor: color + '20',
          pointRadius: 2,
          tension: 0.25,
          fill: true
        }]
      };
      ensureChart(id, 'line', chartData, { 
        responsive: true, 
        plugins: { legend: { display: false }}, 
        scales: { 
          x: { grid: { display: false } }, 
          y: { 
            grid: { color: 'rgba(0,0,0,0.05)' },
            beginAtZero: true,
            max: 100
          } 
        } 
      });
    },
    stackedLine: function(id, labels, datasets){
      const colors = ['#005358','#2b6c70','#4b8588','#7da8ab','#0b7d77','#3d8b8e','#9cc7c9','#1f6b6f','#74a4a6','#b5d3d4'];
      const chartData = {
        labels,
        // Blazor's JS interop serializes object property names to camelCase by default.
        // Support both PascalCase (C# anonymous object) and camelCase (serialized) keys.
        datasets: datasets.map((dataset, i) => ({
          label: (dataset && (dataset.Label ?? dataset.label)) ?? 'Unknown',
          data: (dataset && (dataset.Data ?? dataset.data)) ?? [],
          borderColor: colors[i % colors.length],
          backgroundColor: colors[i % colors.length] + '30',
          pointRadius: 0,
          tension: 0.25,
          fill: true
        }))
      };
      ensureChart(id, 'line', chartData, { 
        responsive: true, 
        plugins: { legend: { position: 'bottom' }}, 
        scales: { 
          x: { 
            grid: { display: false },
            stacked: false
          }, 
          y: { 
            grid: { color: 'rgba(0,0,0,0.05)' },
            beginAtZero: true,
            stacked: false
          } 
        } 
      });
    },
    groupedBar: function(id, languages, models, data, label){
      const colors = ['#005358','#2b6c70','#4b8588','#7da8ab','#0b7d77','#3d8b8e','#9cc7c9','#1f6b6f','#74a4a6','#b5d3d4'];
      const chartData = {
        labels: languages,
        datasets: models.map((model, i) => ({
          label: model,
          data: languages.map(lang => data[lang] ? (data[lang][model] || 0) : 0),
          backgroundColor: colors[i % colors.length] + '80',
          borderColor: colors[i % colors.length],
          borderWidth: 1,
          borderRadius: 2
        }))
      };
      ensureChart(id, 'bar', chartData, { 
        responsive: true, 
        plugins: { legend: { position: 'bottom' }}, 
        scales: { 
          x: { grid: { display: false } }, 
          y: { 
            grid: { color: 'rgba(0,0,0,0.05)' },
            beginAtZero: true
          } 
        } 
      });
    },
    sparkline: function(id, data, color = '#005358'){
      const chartData = {
        labels: data.map((_, i) => ''),
        datasets: [{
          data: data,
          borderColor: color,
          backgroundColor: 'transparent',
          pointRadius: 0,
          pointHoverRadius: 0,
          tension: 0.4,
          fill: false,
          borderWidth: 1.5
        }]
      };
      ensureChart(id, 'line', chartData, { 
        responsive: true,
        maintainAspectRatio: false,
        plugins: { 
          legend: { display: false },
          tooltip: { enabled: false }
        }, 
        scales: { 
          x: { 
            display: false,
            grid: { display: false }
          }, 
          y: { 
            display: false,
            grid: { display: false },
            beginAtZero: false
          } 
        },
        elements: {
          point: { radius: 0 }
        },
        interaction: {
          intersect: false,
          mode: 'index'
        }
      });
    }
  };
})();