resource "null_resource" "service_action" {
  # Always triggered.
  triggers = {
    random_id = uuid()
  }
  # Use az cli to perform operation on azure resource.
  # TODO: Return state in output after operation is executed if needed.
  provisioner "local-exec" {
    when    = create
    command = <<-EOT
    vm=$(az resource show --id ${local.remote.virtual_machines.service.dsvm.id} -o json)
    vmResourceGroup=$(echo $vm | jq -r .resourceGroup)
    vmName=$(echo $vm | jq -r .name)

    if [ ${var.service_action} == 'start' ]
    then
      az vm start --name $vmName --resource-group $vmResourceGroup
    elif [ ${var.service_action} == 'stop' ]
    then
      az vm deallocate --name $vmName --resource-group $vmResourceGroup
    elif [ ${var.service_action} == 'restart' ]
    then
      az vm restart --name $vmName --resource-group $vmResourceGroup
    else
      exit 1
    fi

    vmState=$(az vm list -g $vmResourceGroup -d --query "[?name=='$vmName'].powerState" -o tsv)    
    echo $vmState
        
    EOT
  }
}